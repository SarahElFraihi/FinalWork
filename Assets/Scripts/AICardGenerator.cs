using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AICardGenerator : MonoBehaviour
{
    [Header("Mode Configuration")]
    public bool useSimulator = false;

    [Header("Groq API Configuration")]
    private string apiKey = "";    
    private string url = "https://api.groq.com/openai/v1/chat/completions";    

    [Header("AI Pool")]
    public int maxPoolSize = 5;
    public Queue<CardData> aiCardPool = new Queue<CardData>();
    private bool isFilling = false;

    void Awake()
    {
        LoadAPIKey();
    }

    void Start()
    {
        // On attend que la clé soit chargée avant de remplir le pool
        FillPool();
    }

    void LoadAPIKey()
    {
        // On génère le chemin automatique vers le dossier secret, que ce soit dans l'éditeur ou dans le build final
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "api_key.txt");

        if (System.IO.File.Exists(filePath))
        {
            // On lit le texte et on retire les espaces ou retours à la ligne invisibles (.Trim())
            apiKey = System.IO.File.ReadAllText(filePath).Trim();
            Debug.Log("<color=green>[Groq Security]</color> Clé API chargée avec succès depuis StreamingAssets !");
        }
        else
        {
            Debug.LogError("<color=red>[Groq Security Error]</color> Fichier api_key.txt introuvable dans StreamingAssets !");
        }
    }

    public void FillPool()
    {
        if (!isFilling && aiCardPool.Count < maxPoolSize)
        {
            StartCoroutine(RefillRoutine());
        }
    }

    IEnumerator RefillRoutine()
    {
        isFilling = true;
        while (aiCardPool.Count < maxPoolSize)
        {
            CardData newCard = null;
            bool waiting = true;

            RequestNewCard((card) => {
                newCard = card;
                waiting = false;
            });

            while (waiting) yield return null;

            if (newCard != null && newCard.cardName != "ERROR CARD")
            {
                aiCardPool.Enqueue(newCard);
                Debug.Log("<color=cyan>[Groq Pool]</color> Carte ajoutée. Total : " + aiCardPool.Count);
            }
            
            yield return new WaitForSeconds(1.0f); // 1 seconde suffit largement avec Groq !
        }
        isFilling = false;
    }

    public void RequestNewCard(System.Action<CardData> callback)
    {
        if (useSimulator)
        {
            callback?.Invoke(CreateCardFromJSON(GetMockAIResponse()));
        }
        else
        {
            StartCoroutine(PostRequest(callback));
        }
    }

    IEnumerator PostRequest(System.Action<CardData> callback)
    {
        string prompt = "Generate a funny and chaotic ghost card for a party game. Return ONLY a valid JSON object. " +
            "You MUST include exactly ONE effect in the 'effects' array. " +
            "Format: { \"cardName\": \"Name\", \"description\": \"Desc\", \"cardType\": 0, \"targetMode\": 1, " +
            "\"effects\": [{ \"effectType\": 0, \"value\": 10 }] }. " +
            "CRITICAL RULES FOR GENERATION:\n" +
            "- cardType: 0=Action, 1=Rule, 2=Special\n" +
            "- targetMode: 0=Self, 1=Chosen (enemy), 6=Everyone\n" +
            "- effectType & value instructions:\n" +
            "  * 0 (Damage): value must be POSITIVE (ex: 15)\n" +
            "  * 1 (Heal): value must be POSITIVE (ex: 10)\n" +
            "  * 2 (Gold): give or steal gold (ex: 20 or -15)\n" +
            "  * 3 (Karma): modify player's alignment (ex: 10 or -10)\n" +
            "  * 4 (Luck): modify luck (ex: 5)\n" +
            "  * 5 (Frozen): skip turn, set value to 1\n" +
            "  * 6 (Burn): set on fire, set value to 1\n" +
            "  * 7 (Poison): poisoned state, set value to 1\n" +
            "  * 8 (Shield): block next attack, set value to 1\n" +
            "  * 9 (Invisible): can't be targeted, set value to 1\n" +
            "  * 10 (Silenced): can't use rules/specials, set value to 1\n" +
            "  * 11 (Wanted): grants gold when attacked, set value to 1\n" +
            "  * 12 (Linked): shares damage with right neighbor, set value to 1\n" +
            "  * 13 (Thorns): reflects flat damage (ex: 5)\n" +
            "  * 14 (HandSize): change max hand size (ex: 4 or 6)\n" +
            "Make the description match the effect chosen. Use double quotes for all keys.";

        GroqRequest req = new GroqRequest();
        req.model = "llama-3.1-8b-instant"; // Ton modèle ultra-rapide
        req.messages = new List<GroqMessage> { new GroqMessage { role = "user", content = prompt } };
        req.response_format = new GroqFormat { type = "json_object" };

        string jsonData = JsonUtility.ToJson(req);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string cleanedJson = CleanGroqResponse(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(cleanedJson))
                    callback?.Invoke(CreateCardFromJSON(cleanedJson));
                else
                    callback?.Invoke(CreateCardFromJSON(GetMockAIResponse()));
            }
            else 
            { 
                Debug.LogError("[Groq Error] " + request.downloadHandler.text);
                callback?.Invoke(CreateCardFromJSON(GetMockAIResponse()));
            }
        }
    }

    string CleanGroqResponse(string raw)
    {
        try 
        {
            GroqResponse res = JsonUtility.FromJson<GroqResponse>(raw);
            return res.choices[0].message.content.Trim();
        }
        catch { return ""; }
    }

    public CardData CreateCardFromJSON(string jsonString)
    {
        AICardJSON data = JsonUtility.FromJson<AICardJSON>(jsonString);
        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        newCard.cardName = data.cardName;
        newCard.description = data.description;
        newCard.type = (CardData.CardType)data.cardType;
        newCard.targetMode = (CardData.TargetMode)data.targetMode;
        newCard.requiresTarget = (newCard.targetMode == CardData.TargetMode.Chosen);

        if (newCard.type == CardData.CardType.Action) newCard.cardColor = new Color(0.9f, 0.1f, 0.1f);
        else if (newCard.type == CardData.CardType.Rule) newCard.cardColor = new Color(1f, 0.85f, 0f);
        else newCard.cardColor = new Color(0.6f, 0.1f, 0.9f);

        foreach (var eff in data.effects)
        {
            CardData.CardEffect newEffect = new CardData.CardEffect { 
                effectType = (CardData.EffectType)eff.effectType, 
                value = eff.value 
            };
            newCard.effects.Add(newEffect);
        }
        return newCard;
    }

    public string GetMockAIResponse()
    {
        return "{ \"cardName\": \"ERROR CARD\", \"description\": \"AI Failed to respond.\", \"cardType\": 0, \"targetMode\": 1, \"effects\": [] }";
    }

    // Classes de sérialisation pour Groq
    [System.Serializable] public class GroqRequest { public string model; public List<GroqMessage> messages; public GroqFormat response_format; }
    [System.Serializable] public class GroqMessage { public string role; public string content; }
    [System.Serializable] public class GroqFormat { public string type; }
    [System.Serializable] public class GroqResponse { public List<GroqChoice> choices; }
    [System.Serializable] public class GroqChoice { public GroqChoiceMessage message; }
    [System.Serializable] public class GroqChoiceMessage { public string content; }

    [System.Serializable] public class AICardJSON { public string cardName; public string description; public int cardType; public int targetMode; public List<AIEffectJSON> effects; }
    [System.Serializable] public class AIEffectJSON { public int effectType; public float value; }
}
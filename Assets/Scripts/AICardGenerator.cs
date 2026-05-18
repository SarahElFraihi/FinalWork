using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AICardGenerator : MonoBehaviour
{
    [Header("Mode Configuration")]
    public bool useSimulator = true;

    [Header("API Configuration")]
    public string apiKey = "YOUR_API_KEY_HERE";
    private string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=";    

    public void RequestNewCard(System.Action<CardData> callback)
    {
        if (useSimulator)
        {
            // Mode hors-ligne : On utilise un Mock immédiatement
            string json = GetMockAIResponse();
            CardData card = CreateCardFromJSON(json);
            callback?.Invoke(card);
        }
        else
        {
            // Mode en ligne : On lance la requête API
            StartCoroutine(PostRequest(callback));
        }
    }

    IEnumerator PostRequest(System.Action<CardData> callback)
    {
        // Correction : On utilise des guillemets simples (') dans le prompt pour éviter de casser le JSON
        string prompt = "Generate a chaotic card for a ghost game. Return ONLY JSON: " +
            "{ 'cardName': 'Name', 'description': 'Desc', 'cardType': 0, 'targetMode': 1, " +
            "'effects': [{ 'effectType': 0, 'value': 10 }] }. " +
            "Types: 0:Action, 1:Rule, 2:Special. Targets: 1:Chosen, 6:Everyone. " +
            "EffectTypes: 0:Damage, 1:Heal, 2:Gold, 5:Frozen, 9:Invisible, 15:TimerMod, 18:GravityFlip. English only.";

        // On s'assure que le JSON envoyé est propre
        string jsonData = "{\"contents\":[{\"parts\":[{\"text\":\"" + prompt + "\"}]}]}";
        
        using (UnityWebRequest request = new UnityWebRequest(url + apiKey, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Rappel : SendWebRequest avec un "S" majuscule !
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                CardData newCard = CreateCardFromJSON(CleanGeminiResponse(request.downloadHandler.text));
                callback?.Invoke(newCard);
            }
            else 
            { 
                Debug.LogError("API Error: " + request.error);
                // Affiche la réponse du serveur pour voir le détail de l'erreur 400
                Debug.LogError("Response: " + request.downloadHandler.text); 
                callback?.Invoke(CreateCardFromJSON(GetMockAIResponse()));
            }
        }
    }

    string CleanGeminiResponse(string raw)
    {
        try 
        {
            // Unity déballe toute la structure de Google automatiquement
            GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(raw);
            string aiText = res.candidates[0].content.parts[0].text;

            // Si l'IA a mis des balises ```json ... ```, on les retire
            if (aiText.Contains("```json"))
            {
                aiText = aiText.Replace("```json", "").Replace("```", "");
            }
            
            return aiText.Trim();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur de nettoyage JSON : " + e.Message);
            return GetMockAIResponse(); // Secours en cas de plantage
        }
    }

    public CardData CreateCardFromJSON(string jsonString)
    {
        AICardJSON data = JsonUtility.FromJson<AICardJSON>(jsonString);
        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        
        newCard.cardName = data.cardName;
        newCard.description = data.description;
        newCard.type = (CardData.CardType)data.cardType;
        newCard.targetMode = (CardData.TargetMode)data.targetMode;

        // --- GESTION DES COULEURS (Visuelle pour Ahmed et Lisa) ---
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
        string[] mocks = {
            "{ \"cardName\": \"Lost Wallet\", \"description\": \"Steals 10 gold.\", \"cardType\": 0, \"targetMode\": 1, \"effects\": [{ \"effectType\": 2, \"value\": 10 }] }",
            "{ \"cardName\": \"Blizzard\", \"description\": \"Freezes everyone!\", \"cardType\": 0, \"targetMode\": 6, \"effects\": [{ \"effectType\": 5, \"value\": 1 }] }"
        };
        return mocks[Random.Range(0, mocks.Length)];
    }

    [System.Serializable] public class AICardJSON { public string cardName; public string description; public int cardType; public int targetMode; public List<AIEffectJSON> effects; }
    [System.Serializable] public class AIEffectJSON { public int effectType; public float value; }
    [System.Serializable] public class GeminiResponse { public List<Candidate> candidates; }
    [System.Serializable] public class Candidate { public GeminiContent content; }
    [System.Serializable] public class GeminiContent { public List<Part> parts; }
    [System.Serializable] public class Part { public string text; }

}
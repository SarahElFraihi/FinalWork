using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class AICardGenerator : MonoBehaviour
{
    [Header("Ollama Local Configuration")]
    private string url = "http://localhost:11434/api/generate";    

    [Header("AI Pool")]
    public int maxPoolSize = 10; 
    public Queue<CardData> aiCardPool = new Queue<CardData>();
    private bool isFilling = false;

   private static AICardGenerator instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        FillPool();
    }

    public void FillPool()
    {
        if (!isFilling && aiCardPool.Count < maxPoolSize)
        {
            StartCoroutine(RefillBatchRoutine());
        }
    }

    IEnumerator RefillBatchRoutine()
    {
        isFilling = true;

        while (aiCardPool.Count < maxPoolSize)
        {
            bool waiting = true;
            List<AICardJSON> generatedCards = null;

            StartCoroutine(PostBatchRequest((cards) => {
                generatedCards = cards;
                waiting = false;
            }));

            while (waiting) yield return null;

            if (generatedCards != null && generatedCards.Count == 3)
            {
                foreach (AICardJSON cardData in generatedCards)
                {
                    CardData newCard = ConvertJSONToCardData(cardData);
                    if (newCard != null)
                    {
                        aiCardPool.Enqueue(newCard);
                        Debug.Log("<color=cyan>[Llama Pool]</color> Card added: " + newCard.cardName);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(0.2f);
        }

        isFilling = false;
    }

    IEnumerator PostBatchRequest(System.Action<List<AICardJSON>> callback)
    {
        // === ÉTAPES DES PROBABILITÉS : TRI PAR CATÉGORIE ===
        int[] actionEffects = { 0, 1, 2 };          // Dégâts, Soin, Vol d'or
        int[] ruleEffects = { 15, 16 };            // Lois (Timer, Gravité)
        int[] specialEffects = { 5, 6, 7, 8, 11 };  // États (Gel, Feu, Poison, Bouclier, Silence)
        
        int[] chosenEffects = new int[3];
        int[] forcedTypes = new int[3];
        int[] forcedTargets = new int[3];
        float[] forcedValues = new float[3];
        string[] instructions = new string[3];

        for (int i = 0; i < 3; i++)
        {
            float roll = Random.Range(0f, 100f);
            int eff = 0;

            if (roll < 60f)
            {
                eff = actionEffects[Random.Range(0, actionEffects.Length)];
            }
            else if (roll < 80f) 
            {
                eff = ruleEffects[Random.Range(0, ruleEffects.Length)];
            }
            else
            {
                eff = specialEffects[Random.Range(0, specialEffects.Length)];
            }

            chosenEffects[i] = eff;
            
            if (eff == 15 || eff == 16)
            {
                forcedTypes[i] = 1;
                forcedTargets[i] = 6;
                forcedValues[i] = (eff == 15) ? 15f : 1f;
                instructions[i] = (eff == 15) ? "setting the next round timer to its maximum duration of 15 seconds" : "flipping gravity or turn order";
            }
            else if (eff == 5 || eff == 6 || eff == 7 || eff == 8 || eff == 11)
            {
                forcedTypes[i] = 2;
                forcedValues[i] = 1f;
                forcedTargets[i] = (eff == 8) ? 0 : 1;
                if (eff == 5) instructions[i] = "freezing an opponent to make them skip their entire next turn";
                else if (eff == 6) instructions[i] = "setting an opponent on fire to deal damage over time";
                else if (eff == 7) instructions[i] = "poisoning an opponent with toxic slime to deal damage each turn";
                else if (eff == 8) instructions[i] = "giving yourself a protective shield that blocks the next attack";
                else if (eff == 11) instructions[i] = "silencing an opponent to prevent them from playing Yellow or Purple cards next turn";
            }
            else
            {
                forcedTypes[i] = 0;
                forcedTargets[i] = 1;
                forcedValues[i] = 35f;
                if (eff == 0) instructions[i] = "a spooky offensive attack dealing 35 damage";
                else if (eff == 1) instructions[i] = "ghostly healing or restoration of 25 health points";
                else if (eff == 2) instructions[i] = "stealing 10 gold from the target opponent";
            }
        }

        string prompt = "You are a hilarious ghost party game designer. Generate a batch of EXACTLY 3 unique spooky cards.\n" +
                        "For each card, your description MUST be ultra-short, maximum 10 words to fit the UI template.\n\n" +
                        $"Card 1 theme: {instructions[0]}\n" +
                        $"Card 2 theme: {instructions[1]}\n" +
                        $"Card 3 theme: {instructions[2]}\n\n" +
                        "You MUST follow this exact JSON structure down to lowercase keys:\n" +
                        "{\n" +
                        "  \"cards\": [\n" +
                        "    { \"cardName\": \"Unique ghost pun title\", \"description\": \"Max 10 words phrase\" },\n" +
                        "    { \"cardName\": \"Unique ghost pun title\", \"description\": \"Max 10 words phrase\" },\n" +
                        "    { \"cardName\": \"Unique ghost pun title\", \"description\": \"Max 10 words phrase\" }\n" +
                        "  ]\n" +
                        "}\n\n" +
                        "Return ONLY the raw JSON object, no markdown, no code blocks.";

        OllamaRequest payload = new OllamaRequest();
        payload.model = "llama3";
        payload.prompt = prompt;
        payload.format = "json"; 
        payload.stream = false;
        
        payload.options = new OllamaOptions();
        payload.options.num_predict = 400; 
        payload.options.temperature = 0.9f;
        payload.options.seed = Random.Range(1, 999999);

        string jsonBody = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    OllamaResponse responseContainer = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                    LlamaBatchResponse batchContainer = JsonUtility.FromJson<LlamaBatchResponse>(responseContainer.response);
                    
                    if (batchContainer != null && batchContainer.cards != null && batchContainer.cards.Count == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            batchContainer.cards[i].cardType = forcedTypes[i];
                            batchContainer.cards[i].targetMode = forcedTargets[i];
                            batchContainer.cards[i].effects = new List<AIEffectJSON>();
                            
                            AIEffectJSON eff = new AIEffectJSON();
                            eff.effectType = chosenEffects[i];
                            eff.value = forcedValues[i];
                            batchContainer.cards[i].effects.Add(eff);
                        }
                        callback?.Invoke(batchContainer.cards);
                    }
                    else
                    {
                        callback?.Invoke(null);
                    }
                }
                catch
                {
                    callback?.Invoke(null);
                }
            }
            else 
            { 
                callback?.Invoke(null);
            }
        }
    }

    public CardData ConvertJSONToCardData(AICardJSON data)
    {
        if (data == null || string.IsNullOrEmpty(data.cardName)) return null;

        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        newCard.cardName = data.cardName;
        newCard.description = data.description;
        newCard.type = (CardData.CardType)data.cardType;
        newCard.targetMode = (CardData.TargetMode)data.targetMode;
        newCard.requiresTarget = (newCard.targetMode == CardData.TargetMode.Chosen);

        if (newCard.type == CardData.CardType.Action) newCard.cardColor = new Color(0.9f, 0.1f, 0.1f);
        else if (newCard.type == CardData.CardType.Rule) newCard.cardColor = new Color(1f, 0.85f, 0f);
        else newCard.cardColor = new Color(0.6f, 0.1f, 0.9f);

        if (data.effects != null)
        {
            foreach (var eff in data.effects)
            {
                CardData.CardEffect newEffect = new CardData.CardEffect { 
                    effectType = (CardData.EffectType)eff.effectType, 
                    value = eff.value 
                };
                newCard.effects.Add(newEffect);
            }
        }
        return newCard;
    }

    [System.Serializable]
    public class OllamaRequest { public string model; public string prompt; public string format; public bool stream; public OllamaOptions options; }

    [System.Serializable]
    public class OllamaOptions { public int num_predict; public float temperature; public int seed; }

    [System.Serializable]
    public class OllamaResponse { public string response; }

    [System.Serializable]
    public class LlamaBatchResponse { public List<AICardJSON> cards; }

    [System.Serializable] 
    public class AICardJSON { public string cardName; public string description; public int cardType; public int targetMode; public List<AIEffectJSON> effects; }
    
    [System.Serializable] 
    public class AIEffectJSON { public int effectType; public float value; }
}
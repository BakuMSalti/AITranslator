using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using TMPro;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using ArabicSupport;
using UnityEngine.Windows;

public class AIScript : MonoBehaviour
{
    // Set these TMP references in the Unity Inspector
    public TMP_InputField userInputField;  // To capture user input (using TMP)
    public TMP_Text resultText;            // To display the translated text (using TMP)
    public TMP_Dropdown languageDropdown;

    public TMP_InputField userLat;
    public TMP_InputField userLon;
    private const string API_KEY = "f0f34f773cde47469354e5c479c5e85d";
    private const string ENDPOINT = "https://toyzassistantv2.openai.azure.com/openai/deployments/airesourcemodeltestgp4o/chat/completions?api-version=2024-02-15-preview";

    private const string apiKey = "AIzaSyB31qMX0eLKoOtcF17q-xq5Y9YRUyv859w";
    private const string googleGeolocationUrl = "https://www.googleapis.com/geolocation/v1/geolocate?key=" + apiKey;
    // Classes to map the AI response
    public class OpenAIResponse
{
    public Choice[] choices { get; set; }
}

public class Choice
{
    public MessageContent message { get; set; }
}

public class MessageContent
{
    public string content { get; set; }
}

    public bool ArabicChecker(TMP_Text textComponent)
    {
        // Get the selected language from the dropdown menu
        string selectedLanguage = languageDropdown.options[languageDropdown.value].text;

        // List of RTL languages
        string[] rtlLanguages = { "Arabic", "Hebrew", "Persian", "Urdu" };


        // Check if the selected language is in the RTL list
        if (System.Array.Exists(rtlLanguages, lang => lang == selectedLanguage))
        {
            Debug.Log("RTL detected");
            TMP_FontAsset rtlFont = Resources.Load<TMP_FontAsset>("TSMorabaat-Bold SDF");
            if (rtlFont != null)
            {
                textComponent.font = rtlFont;
            }
            return true;
        }
        if (selectedLanguage == "Chinese (Simplified)" || selectedLanguage == "Cantonese (Simplified)")
        {
            // Switch to the ZCoolKuai font for Chinese languages
            TMP_FontAsset zCoolKuaiFont = Resources.Load<TMP_FontAsset>("ZCOOLKuaiLe-Regular SDF");
            if (zCoolKuaiFont != null)
            {
                textComponent.font = zCoolKuaiFont;
            }
            textComponent.isRightToLeftText = false;
            return false;
        }
        if (selectedLanguage == "Japanese")
        {
            TMP_FontAsset japaneseFont = Resources.Load<TMP_FontAsset>("ReggaeOne-Regular SDF");
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            textComponent.isRightToLeftText = false;
            return false;
        }
        if (selectedLanguage == "Chinese (Traditional)" || selectedLanguage == "Cantonese (Traditional)")
        {
            TMP_FontAsset chntra = Resources.Load<TMP_FontAsset>("ChocolateClassicalSans-Regular SDF");
            if (chntra != null)
            {
                textComponent.font = chntra;
            }
            textComponent.isRightToLeftText = false;
            return false;
        }
        if (selectedLanguage == "Korean")
        {
            TMP_FontAsset korean = Resources.Load<TMP_FontAsset>("Gugi-Regular SDF");
            if (korean != null)
            {
                textComponent.font = korean;
            }
            textComponent.isRightToLeftText = false;
            return false;
        }
        else
        {
            textComponent.isRightToLeftText = false;
            return false;
        }
    }

    public string PuncFixer(string txt)
    {
        if (string.IsNullOrEmpty(txt))
        {
            return txt;
        }

        // Replace all occurrences of Asian puncuation with Latin ones
        txt = txt.Replace("，", ", ");
        txt = txt.Replace("？", "? ");
        txt = txt.Replace("。", ". ");
        txt = txt.Replace("！", "! ");
        txt = txt.Replace("：", ": ");
        txt = txt.Replace("（", "(");
        txt = txt.Replace("）", ")");
        txt = txt.Replace("；", "; ");
        txt = txt.Replace("、", ", ");
        txt = txt.Replace("「", "\" ");
        txt = txt.Replace("」", "\" ");

        return txt;
    }
    public async void RequestTranslation()
    {
        string textToTranslate = userInputField.text;
        string selectedLanguage = languageDropdown.options[languageDropdown.value].text;

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("api-key", API_KEY);

            var payload = new
            {
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = "You are an AI assistant that helps people translate text."
                            }
                        }
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = $"Translate the following text into {selectedLanguage} based on the common colloquial, informal dialect most commonly found at the following corrdinates: Latitude: {userLat.text}, Longitude: {userLon.text}. " +
                                $"If the area specified does not have speak the specified language, use the general most popular colloquial dialect. (IE: If it was Spanish in Japan, just use colloquial Latin American Spanish since its the most popular dialect) " +
                                //"When it comes to puncuation (such as commas, question marks, etc), only use the Latin versions of these characters for the sake of font. (IE: If translating into Japanese, use the Latin Comma U+002C instead of the Full-Width Comma U+FF0C). Only make an exception if you're translating into a right-to-left language such as Arabic." +
                                //$"Only include the translated text and the name of the area the coordinates map too (IE: Madrid, Spain 'Buenos dias'). \n {textToTranslate}"
                                $"Only include the translated text (IE:'Buenos dias'). \n {textToTranslate}"
                            }
                        }
                    }
                },
                temperature = 0.7,
                top_p = 0.95,
                max_tokens = 800,
                stream = false
            };

            var response = await httpClient.PostAsync(ENDPOINT, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var rawResponse = await response.Content.ReadAsStringAsync();
                OpenAIResponse responseData = JsonConvert.DeserializeObject<OpenAIResponse>(rawResponse);
                string chatResponse = responseData.choices[0].message.content;
                Debug.Log(chatResponse);
                if (ArabicChecker(resultText))
                {
                    resultText.text = ArabicFixer.Fix(chatResponse);
                }
                else
                {
                    chatResponse = PuncFixer(chatResponse);
                    resultText.text = chatResponse;
                }
            }
            else
            {
                resultText.text = $"Error: {response.StatusCode}, {response.ReasonPhrase}";
            }
        }
    }

    void Start()
    {
        StartCoroutine(GetLocation());
    }
    IEnumerator GetLocation()
    {
        // Set up a POST request to the Google Geolocation API
        UnityWebRequest request = new UnityWebRequest(googleGeolocationUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");  // Empty body for basic geolocation
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            Debug.Log("Received: " + request.downloadHandler.text);

            // Parse the JSON response to get the latitude and longitude
            JObject json = JObject.Parse(request.downloadHandler.text);
            float latitude = (float)json["location"]["lat"];
            float longitude = (float)json["location"]["lng"];
            Debug.Log($"Latitude: {latitude}, Longitude: {longitude}");

            // Display the coordinates in the TextMeshPro TextField
            userLat.text = $"{latitude}";
            userLon.text = $"{longitude}";
        }
    }

}

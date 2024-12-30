using ICSharpCode.SharpZipLib.Core;
using ModTool;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using ModIO.Implementation;
using System.Collections;
using Steamworks;

namespace ModIO
{
    public class ModIOManager
    {
        // Generating Dummy Mods
        static readonly byte[] Megabyte = new byte[1024 * 1024];
        static readonly System.Random RandomBytes = new System.Random();

        // Searching for Mods
        static ModProfile[] allMods;
        static ModProfile[] subscribedMods;

        // Installing Mods
        static string downloadName = "";
        static float downloadProgress;

        public delegate void DownloadedEvent(long id, string modName);
        public static event DownloadedEvent OnDownloaded;

        public delegate void RefreshEvent();
        public static event RefreshEvent OnSubscribedMods;

        public delegate void AuthEvent();
        public static event AuthEvent OnAuthEvent;

        public static bool afterAuthCheck;

        private HAuthTicket authTicket;
        private static string steamToken;
        private static CallResult<EncryptedAppTicketResponse_t> encryptedAppTicketCallResult;

        #region Initialization

        public static IEnumerator Start()
        {
            Debug.Log("Starting steam and modio");
            SteamAPI.Init();
            while (!SteamManager.Initialized)
                yield return null;
            {
                Debug.Log("Starting initialized");
                Result result = ModIOUnity.InitializeForUser("default");
                if (!result.Succeeded())
                    yield return 0;

                Debug.Log("ModIO plugin initialized!");

                byte[] k_unSecretData = System.BitConverter.GetBytes(0x5444);
                Debug.Log("Requesting encrypted app ticket.");
                SteamAPICall_t handle = SteamUser.RequestEncryptedAppTicket(k_unSecretData, sizeof(uint));
                encryptedAppTicketCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(OnInit);
                encryptedAppTicketCallResult.Set(handle);
            }
        }

        static async void OnInit(EncryptedAppTicketResponse_t encryptedResponse, bool bIOFailure)
        {
            Debug.Log("OnEncryptedAppTicketResponse called.");

            byte[] token = new byte[1024];
            uint tokenSize = 0;
            SteamUser.GetEncryptedAppTicket(token, token.Length, out tokenSize);


            // Resize token array to the actual size
            byte[] authToken = new byte[tokenSize];
            Array.Copy(token, authToken, tokenSize);
            steamToken = System.Convert.ToBase64String(authToken);
            TermsOfUse termsHash;

            ModIOUnity.GetTermsOfUse(async (response) =>
            {
                if (response.result.Succeeded())
                {
                    termsHash = response.value;

                    await ModIOUnityAsync.AuthenticateUserViaSteam(steamToken, null, termsHash.hash);
                    Debug.Log("accepted");
                }
                else
                {
                    Debug.LogError("Failed to authenticate: " + response);
                }
            }
           );
            Result result = await ModIOUnityAsync.IsAuthenticated();
            if (result.Succeeded())
            {
                OnAuth();

                return;
            }
        }

        #region Authentication

        public static async void RequestAuthCode()
        {
            string email = "kevin.tanoh.k@gmail.com";
            Result result = await ModIOUnityAsync.RequestAuthenticationEmail(email);
            if (!result.Succeeded())
            {
                Debug.LogError($"RequestAuthenticationEmail failed: {result.message}");

                return;
            }

            Debug.Log($"Authentication email sent to: {email}");

        }

        public static async void SubmitAuthCode(string code)
        {
            Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(code);
            if (!result.Succeeded())
            {
                Debug.LogError($"SubmitEmailSecurityCode failed: {result.message}");

                return;
            }

            OnAuth();
        }

        #endregion


        static async void OnAuth()
        {
            ResultAnd<UserProfile> result = await ModIOUnityAsync.GetCurrentUser();
            if (!result.result.Succeeded())
            {
                Debug.LogError($"GetCurrentUser failed: {result.result.message}");
            }

            Debug.Log($"Authenticated user: {result.value.username}");

            allMods = await GetAllMods();
            Debug.Log($"Available mods:\n{string.Join("\n", allMods.Select(mod => $"{mod.name} (id: {mod.id.id})"))}");

            Result resultUpdate = await ModIOUnityAsync.FetchUpdates();
            if (!resultUpdate.Succeeded())
            {
                Debug.LogError($"FetchUpdates failed: {resultUpdate.message}");
            }

            subscribedMods = GetSubscribedMods();

            Debug.Log($"Subscribed mods:\n{(subscribedMods.Length > 0 ? string.Join("\n", subscribedMods.Select(mod => $"{mod.name} (id: {mod.id.id})")) : "None")}");

            if (allMods.Length > 0)
            {
                int index = Array.FindIndex(allMods, mod => mod.name == "Ten New Missions");
                if (index != -1)
                    await SubscribeToMod(allMods[index]);
                else
                    Debug.Log("Couldn't find Ten New Missions mod, not subscribing");
            }
            else
                Debug.Log("No mods found, not subscribing");

            EnableModManagement();
            afterAuthCheck = true;
        }

        #endregion



        #region Uploading Mods

        static async Task UploadMod(string name, string summary, Texture2D logo, string path)
        {
            Debug.Log($"Starting upload: {name}");

            ModProfileDetails details = new ModProfileDetails
            {
                name = name,
                summary = summary,
                logo = logo,
            };

            ResultAnd<ModId> resultCreate = await ModIOUnityAsync.CreateModProfile(ModIOUnity.GenerateCreationToken(), details);
            if (!resultCreate.result.Succeeded())
            {
                Debug.LogError($"CreateModProfile failed: {resultCreate.result.message}");

                return;
            }

            ModfileDetails modFile = new ModfileDetails
            {
                modId = resultCreate.value,
                directory = path,
            };

            float progress = 0f;

            Task<Result> taskUpload = ModIOUnityAsync.UploadModfile(modFile);
            while (!taskUpload.IsCompleted)
            {
                ProgressHandle progressHandle = ModIOUnity.GetCurrentUploadHandle();

                if (!Mathf.Approximately(progressHandle.Progress, progress))
                {
                    progress = progressHandle.Progress;
                    Debug.Log($"Uploading: {name} ({Mathf.RoundToInt(progress * 100)}%)");
                }

                await Task.Delay(1000);
            }

            if (!taskUpload.Result.Succeeded())
            {
                Debug.LogError($"UploadModfile failed: {taskUpload.Result.message}");

                return;
            }

            Debug.Log($"Finished upload: {name}");
        }

        #endregion

        #region Searching for Mods


        static async Task<ModProfile[]> GetAllMods()
        {
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return Array.Empty<ModProfile>();
            }

            return resultAnd.value.modProfiles;
        }

        public static async Task<ModProfile> FindModByName(string name)
        {
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return new ModProfile();
            }

            // Will search for all the subscribers mods first, then will search in the mod.io DataBase
            try
            {
                var matchingMod = subscribedMods.FirstOrDefault(mod => mod.name == name);
                if (matchingMod.id != 0)
                {
                    return matchingMod;
                }
                else
                {
                    return SearchDataBase(name, resultAnd);
                }

            }
            catch (ArgumentNullException ex)
            {
                return SearchDataBase(name, resultAnd);
            }

        }
        public static async Task<ModProfile> FindModByNameAndAuthor(string name, string author)
        {
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(new SearchFilter());
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return new ModProfile();
            }

            // Will search for all the subscribers mods first, then will search in the mod.io DataBase
            try
            {
                var matchingMod = subscribedMods.FirstOrDefault(mod => mod.name == name && mod.creator.username == author);
                if (matchingMod.id != 0)
                {
                    return matchingMod;
                }
                else
                {
                    return SearchDataBase(name, resultAnd);
                }

            }
            catch (ArgumentNullException ex)
            {
                return SearchDataBase(name, resultAnd);
            }

        }
        public static async Task<ModProfile[]> FindModsByTag(string tag)
        {
            SearchFilter filter = new SearchFilter();
            filter.AddTag(tag);
            ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(filter);
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"GetMods failed: {resultAnd.result.message}");

                return Array.Empty<ModProfile>();
            }

            return resultAnd.value.modProfiles;

        }
        public static ModProfile SearchDataBase(string name, ResultAnd<ModPage> resultAnd)
        {
            var matchingMod = resultAnd.value.modProfiles.FirstOrDefault(mod => mod.name == name);
            if (matchingMod.id.id != 0)
            {
                return matchingMod;
            }
            else
            {
                return new ModProfile();
            }
        }

        #endregion


        #region Getting Subscribed Mods

        public static ModProfile[] GetSubscribedMods()
        {
            SubscribedMod[] subscribed = ModIOUnity.GetSubscribedMods(out Result result);
            if (!result.Succeeded())
            {
                Debug.LogError($"GetSubscribedMods failed: {result.message}");

                return Array.Empty<ModProfile>();
            }
            return subscribed.Select(mod => mod.modProfile).ToArray();
        }

        #endregion

        #region Subscribing to Mods

        public static async Task SubscribeToMod(ModProfile modToSubscribe)
        {
            if (subscribedMods.Any(mod => mod.id == modToSubscribe.id))
                return;

            Result result = await ModIOUnityAsync.SubscribeToMod(modToSubscribe.id);
            if (!result.Succeeded())
            {
                Debug.LogError($"SubscribeToMod failed: {result.message}");

                return;
            }

            Debug.Log($"Subscribed to mod: {allMods.First(mod => mod.id == modToSubscribe.id).name}");
        }

        #endregion

        #region Installing Mods

        static public void EnableModManagement()
        {
            void HandleModManagementEvent(ModManagementEventType eventType, ModId modId, Result eventResult)
            {
                switch (eventType)
                {
                    case ModManagementEventType.DownloadStarted:
                        downloadName = allMods.First(mod => mod.id == modId).name;
                        Debug.Log($"Downloading {downloadName}");
                        break;
                    case ModManagementEventType.Downloaded:
                        Debug.Log($"Downloaded {downloadName}");
                        subscribedMods = GetSubscribedMods();
                        downloadName = string.Empty;
                        break;
                    case ModManagementEventType.DownloadFailed:
                        Debug.Log($"Download failed {downloadName}");
                        downloadName = string.Empty;
                        break;
                }
            }

            ModIOUnity.EnableModManagement(HandleModManagementEvent);
        }

        static public void Update()
        {
            SteamAPI.RunCallbacks();
            if (downloadName.Length == 0)
                return;

            ProgressHandle progress = ModIOUnity.GetCurrentModManagementOperation();

            if (Mathf.Approximately(progress.Progress, downloadProgress))
                return;

            downloadProgress = progress.Progress;
            Debug.Log($"Downloading {downloadName} ({Mathf.RoundToInt(downloadProgress * 100)}%)");
        }

        #endregion

        #region Image Downloading
        public static async Task<Texture2D> DownloadImage(ModProfile modProfile)
        {
            ResultAnd<Texture2D> resultAnd = await ModIOUnityAsync.DownloadTexture(modProfile.logoImage320x180);
            if (!resultAnd.result.Succeeded())
            {
                Debug.LogError($"DownloadTexture failed: {resultAnd.result.message}");

                return null;
            }

            Texture2D logo = resultAnd.value;
            return logo;
        }
        #endregion

        public static ModProfile[] SubscribedMods { get { return subscribedMods; } }

    }

}

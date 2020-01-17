using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// for web comms
using UnityEngine.Networking;

// for load/save local
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace EcoBuilder
{
    public partial class GameManager : MonoBehaviour
    {
        /////////////////////
        // data collection //
        /////////////////////

        [Serializable]
        public class PlayerDetails
        {
            public string username = "";
            public string password = "";
            public string email = "";

            public int age = 0;
            public int gender = 0;
            public int education = 0;
            public enum Team { None=0, Wolf, Lion }
            public Team team = Team.None;

            public bool reverseDrag = true;
            public bool dontAskForLogin = false;
            public bool syncedRemote = false;

            public Dictionary<int, int> highScores = new Dictionary<int, int>();
        }
        [SerializeField] PlayerDetails player = null;
        public PlayerDetails.Team PlayerTeam { get { return player.team; } }

        public bool ConstrainTrophic { get { return player.team != GameManager.PlayerDetails.Team.Lion; } }
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForLogin { get { return player.team==0 && !player.dontAskForLogin; } }

        static string playerPath;
        public void InitPlayer()
        {
            // ugh unity annoying so hard-coded
#if !UNITY_WEBGL
            playerPath = Application.persistentDataPath+"/player.data";
#else
            playerPath = null;
#endif
            // DeletePlayerDetailsLocal();
            bool loaded = LoadPlayerDetailsLocal(); 
        }


        private bool SavePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return false;
#endif

            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Create(playerPath);
                bf.Serialize(file, player);
                file.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message);
                return false;
            }
        }
        private bool LoadPlayerDetailsLocal()
        {
#if UNITY_WEBGL
                return false;
#endif
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Open(playerPath, FileMode.Open);
                player = (PlayerDetails)bf.Deserialize(file);
                file.Close();

                return true;
            }
            catch (Exception e)
            {
                print("handled exception: " + e.Message);
                return false;
            }
        }
        private void DeletePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return;
#endif
            try
            {
                File.Delete(playerPath);
            }
            catch (Exception e)
            {
                print("no save file to delete: " + e.Message);
            }
        }
        public void ResetSaveData()
        {
            print("TODO: something else");
            DeletePlayerDetailsLocal();
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }



        ///////////////////
        // web things
        static readonly string server = "127.0.0.1/ecobuilder/";
        IEnumerator SendHttpPost(string address, WWWForm form, Action<bool, string> OnCompletion)
        {
            using (var p = UnityWebRequest.Post(address, form))
            {
                print("TODO: shade and skip/timeout");
                yield return p.SendWebRequest();
                if (p.isNetworkError || p.isHttpError)
                {
                    OnCompletion(false, p.error);
                }
                else
                {
                    OnCompletion(true, p.downloadHandler.text);
                }
            }
        }
        public void RegisterLocal(string username, string password, string email)
        {
            player.username = username;
            player.password = password;
            player.email = email;
            SavePlayerDetailsLocal();
        }
        public void RegisterRemote(Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            form.AddField("username", Encryption.Encrypt(player.username));
            form.AddField("password", Encryption.Encrypt(player.password));
            form.AddField("email", Encryption.Encrypt(player.email));
            StartCoroutine(SendHttpPost(server+"register.php", form, (b,s)=>OnCompletion(b)));
        }
        public void SetDemographicsLocal(int age, int gender, int education)
        {
            player.age = age;
            player.gender = gender;
            player.education = education;
            SavePlayerDetailsLocal();
        }
        public void SetDemographicsRemote(Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            form.AddField("username", Encryption.Encrypt(player.username));
            form.AddField("password", Encryption.Encrypt(player.password));
            form.AddField("age", Encryption.Encrypt(player.age.ToString()));
            form.AddField("gender", Encryption.Encrypt(player.gender.ToString()));
            form.AddField("education", Encryption.Encrypt(player.education.ToString()));
            StartCoroutine(SendHttpPost(server+"demographics.php", form, (b,s)=>OnCompletion(b)));
            print("TODO: no need to pause, but mark as 'need to send' later if not");
        }
        public void SetTeamLocal(PlayerDetails.Team team)
        {
            player.team = team;
            SavePlayerDetailsLocal();
        }
        public void SetTeamRemote(Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            form.AddField("username", Encryption.Encrypt(player.username));
            form.AddField("password", Encryption.Encrypt(player.password));
            form.AddField("team", Encryption.Encrypt(((int)player.team).ToString()));
            StartCoroutine(SendHttpPost(server+"team.php", form, (b,s)=>OnCompletion(b)));
        }
        public void SetDragDirectionLocal(bool reversed)
        {
            player.reverseDrag = reversed;
            SavePlayerDetailsLocal();
        }
        public void SetDragDirectionRemote(Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            form.AddField("username", Encryption.Encrypt(player.username));
            form.AddField("password", Encryption.Encrypt(player.password));
            form.AddField("reversed", Encryption.Encrypt(player.reverseDrag?"1":"0"));
            StartCoroutine(SendHttpPost(server+"drag.php", form, (b,s)=>OnCompletion(b)));
            print("TODO: save on server as well");
        }
        public void LoginRemote(string username, string password, Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            form.AddField("username", Encryption.Encrypt(player.username));
            form.AddField("password", Encryption.Encrypt(player.password));
            StartCoroutine(SendHttpPost(server+"login.php", form, (b,s)=>{ OnCompletion(b); if (b) ParseScores(s); }));
            SavePlayerDetailsLocal();
        }
        void ParseScores(string scores)
        {
            print("TODO: parse the scores and place them into player.highScores");
        }
        public Tuple<int,int,int> GetTop3ScoresRemote(int levelIdx)
        {
            print("TODO: get global scores from server");
            return Tuple.Create(14,12,10);
        }
        public bool SendEmailReminder(string username)
        {
            print("TODO:");
            return true;
        }

        public void DontAskAgainForLogin()
        {
            player.dontAskForLogin = true;
            SavePlayerDetailsLocal();
        }
        public void Logout()
        {
            DeletePlayerDetailsLocal();
        }

        // This whole structure is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level.LevelDetails, but that is not possible in a build
        public int GetHighScoreLocal(int levelIdx)
        {
            if (!player.highScores.ContainsKey(levelIdx)) {
                player.highScores[levelIdx] = -1;
            }
            return player.highScores[levelIdx];
        }
        // returns whether new high score is achieved
        public void SaveHighScoreLocal(int levelIdx, int score)
        {
            if (GetHighScoreLocal(levelIdx) < score) {
                player.highScores[levelIdx] = score;
                SavePlayerDetailsLocal();
            }
        }
        public void SavePlaythroughRemote(int levlIdx, int score, double[,] matrix, int[,] actions)
        {
            var bf = new BinaryFormatter();
            var form = new WWWForm();
            form.AddField("idx", PlayedLevel.Details.idx.ToString());
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, matrix);
                form.AddBinaryData("matrix", ms.ToArray());
            }

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, actions);
                form.AddBinaryData("actions", ms.ToArray());
            }

            print("TODO: send actual data");
            // TODO: cut max size of these things if necessary
            //       if sending fails, store locally and try to send next time
        }
        public bool IsLearningFinished {
            get {
                // check if last learning level has been completed
                if (player.highScores[learningLevelPrefabs[learningLevelPrefabs.Count-1].Details.idx] < 1) {
                    return false;
                } else {
                    return true;
                }
            }
        }
    }
}
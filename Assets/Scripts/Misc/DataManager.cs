using System;
using System.Collections.Generic;
using UnityEngine;

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

            public int age = -1;
            public int gender = -1;
            public int education = -1;
            public enum Team { None=-1, Wolf, Lion }
            public Team team = Team.None;

            public bool reverseDrag = true;
            public bool dontAskForLogin = false;

            public Dictionary<int, int> highScores = new Dictionary<int, int>();
        }
        [SerializeField] PlayerDetails player = null;
        public PlayerDetails.Team PlayerTeam { get { return player.team; } }

        public bool ConstrainTrophic { get { return player.team != GameManager.PlayerDetails.Team.Lion; } }
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForRegistration { get { return player.team==PlayerDetails.Team.None && !player.dontAskForLogin; } }

        static string playerPath;
        public void InitPlayer()
        {
            // ugh unity annoying so hard-coded
            playerPath = Application.persistentDataPath+"/player.data";

            DeletePlayerDetailsLocal();
            if (LoadPlayerDetailsLocal() == false) {
                player = new PlayerDetails();
            }
        }
        private bool SavePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return false;
#endif
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Create(playerPath);
                bf.Serialize(file, player);
                file.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("could not save player: " + e.Message);
                return false;
            }
        }
        private bool LoadPlayerDetailsLocal()
        {
#if UNITY_WEBGL
                return false;
#endif
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(playerPath, FileMode.Open);
                player = (PlayerDetails)bf.Deserialize(file);
                file.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("could not load player: " + e.Message);
                return false;
            }
        }
        private void DeletePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return;
#endif
            try {
                File.Delete(playerPath);
            } catch (Exception e) {
                Debug.LogError("could not delete player: " + e.Message);
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
        static readonly string serverURL = "127.0.0.1/ecobuilder/";
        // static readonly string serverURL = "https://www.ecobuildergame.org/Beta/";
        [SerializeField] UI.Postman pat;
        public void RegisterLocal(string username, string password, string email)
        {
            player.username = username;
            player.password = password;
            player.email = email;
            SavePlayerDetailsLocal();
        }
        public void RegisterRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "email", player.email },
                { "__address__", serverURL+"register.php" },
            };
            pat.Post(data, OnCompletion);
        }
        public void SetDemographicsLocal(int age, int gender, int education)
        {
            player.age = age;
            player.gender = gender;
            player.education = education;
            SavePlayerDetailsLocal();
        }
        public void SetDemographicsRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "age", player.age.ToString() },
                { "gender", player.gender.ToString() },
                { "education", player.education.ToString() },
                { "__address__", serverURL+"demographics.php" },
            };
            OnCompletion += (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }
        public void SetTeamLocal(PlayerDetails.Team team)
        {
            player.team = team;
            SavePlayerDetailsLocal();
        }
        public void SetTeamRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "team", ((int)player.team).ToString() },
                { "__address__", serverURL+"team.php" },
            };
            OnCompletion += (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }
        public void SetDragDirectionLocal(bool reversed)
        {
            player.reverseDrag = reversed;
            SavePlayerDetailsLocal();
        }
        public void SetDragDirectionRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "reversed", player.reverseDrag? "1":"0" },
                { "__address__", serverURL+"login.php" },
            };
            OnCompletion += (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }
        public void LoginRemote(string username, string password, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", username },
                { "password", password },
                { "__address__", serverURL+"login.php" },
            };
            pat.Post(data, (b,s)=>{ if (b) ParseLogin(username, password, s); OnCompletion(b,s); });
        }
        void ParseLogin(string username, string password, string returned)
        {
            print(returned);
            player.username = username;
            player.password = password;
            var details = returned.Split(';');
            player.team = (PlayerDetails.Team)int.Parse(details[0]);
            player.reverseDrag = int.Parse(details[1])==1? true:false;

            player.highScores.Clear();
            for (int i=2; i<details.Length; i++)
            {
                var level = details[i].Split(':');
                int idx = int.Parse(level[0]);
                int score = int.Parse(level[1]);
                SaveHighScoreLocal(idx, score);
            }
            SavePlayerDetailsLocal();
        }
        public void SendPasswordResetEmail(string recipient, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "recipient", recipient },
                { "__address__", serverURL+"resetup.php" },
            };
            pat.Post(data, OnCompletion);
        }

        // This whole framework is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level.LevelDetails, but that is not possible in a build
        public int GetHighScoreLocal(int levelIdx)
        {
            if (!player.highScores.ContainsKey(levelIdx)) {
                player.highScores[levelIdx] = 0;
            }
            return player.highScores[levelIdx];
        }
        // returns whether new high score is achieved
        public bool SaveHighScoreLocal(int levelIdx, int score)
        {
            if (GetHighScoreLocal(levelIdx) < score)
            {
                player.highScores[levelIdx] = score;
                SavePlayerDetailsLocal();
                return true;
            }
            return false;
        }
        public void SavePlaythroughRemote(int levelIdx, int score, string matrix, string actions, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "level_index", levelIdx.ToString() },
                { "datetime_ticks", DateTime.Now.Ticks.ToString() },
                { "score", score.ToString() },
                { "__matrix__", matrix },
                { "__actions__", actions },
                { "__address__", serverURL+"playthrough.php" },
            };
            OnCompletion += (b,s)=> { print(s); if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }
        private void SavePost(Dictionary<string, string> data)
        {
#if UNITY_WEBGL
            return;
#endif
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                string path = Application.persistentDataPath+"/"+DateTime.Now.Ticks.ToString();
                while (File.Exists(path + ".post")) {
                    path += "_"; // in case there is somehow more than one file made with the same timestamp
                }
                FileStream file = File.Create(path+".post");
                bf.Serialize(file, data);
                file.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("could not save post: " + e.Message);
            }
        }
        public Tuple<int,int,int> GetTop3ScoresRemote(int levelIdx)
        {
            print("TODO: get global scores from server");
            return Tuple.Create(14,12,10);
        }
        public void OpenPrivacyPolicyInBrowser()
        {
            Application.OpenURL(serverURL+"GDPR_Privacy_Notice.htm");
        }

        public void DontAskAgainForLogin()
        {
            player.dontAskForLogin = true;
            SavePlayerDetailsLocal();
        }
        public void Logout()
        {
            print("TODO: reset?");
            DeletePlayerDetailsLocal();
        }
    }
}
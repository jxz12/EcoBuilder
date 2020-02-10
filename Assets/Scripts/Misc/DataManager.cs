using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// for load/save local
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;

namespace EcoBuilder
{
    public partial class GameManager : MonoBehaviour
    {
        /////////////////////
        // data collection //
        /////////////////////

        [Serializable]
        class PlayerDetails
        {
            public string username = "";
            public string password = "";
            public string email = "";

            public int age = -1;
            public int gender = -1;
            public int education = -1;
            public enum Team { Unassigned=-1, Wolf, Lion, NeverAsk }
            public Team team = Team.Unassigned;

            public bool newsletterConsent = true;
            public bool reverseDrag = true;

            public Dictionary<int, int> highScores = new Dictionary<int, int>();
        }
        [SerializeField] PlayerDetails player = null;

        public bool LoggedIn { get { return player.team==PlayerDetails.Team.Wolf || player.team==PlayerDetails.Team.Lion; }}
        // public bool ConstrainTrophic { get { return player.team != PlayerDetails.Team.Lion; } }
        public bool ConstrainTrophic { get { return false; } }
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForRegistration { get { return player.team==PlayerDetails.Team.Unassigned; } }
        public string Username { get { return player.username; } }

        static string playerPath;
        public void InitPlayer()
        {
            // ugh unity annoying so hard-coded
            playerPath = Application.persistentDataPath+"/player.data";

#if UNITY_EDITOR
            // DeletePlayerDetailsLocal();
#endif
            if (LoadPlayerDetailsLocal() == false) {
                player = new PlayerDetails();
            }
            print("TODO: send data periodically");
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
                player = new PlayerDetails();
            } catch (Exception e) {
                Debug.LogError("could not delete player: " + e.Message);
            }
        }


        ///////////////////
        // web form things

        static readonly string serverURL = "127.0.0.1/ecobuilder/";
        // static readonly string serverURL = "https://www.ecobuildergame.org/Beta/";
        [SerializeField] Postman pat;
        public void RegisterLocal(string username, string password, string email)
        {
            player.username = username;
            player.password = Postman.Encrypt(password);
            player.email = Postman.Encrypt(email);
            SavePlayerDetailsLocal();
        }
        public void RegisterRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "email", player.email },

                // these two are potentially set from someone who did not create an account at first
                { "reversed", player.reverseDrag? "1":"0" },
                { "scores", ComposeRegistrationScores() },

                { "__address__", serverURL+"register.php" },
            };
            pat.Post(data, OnCompletion);
        }
        string ComposeRegistrationScores()
        {
            if (player.highScores.Count == 0) {
                return "";
            }
            var sb = new StringBuilder();
            foreach (var kvp in player.highScores)
            {
                sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append(';');
            }
            sb.Length -= 1;
            return sb.ToString();
        }
        public void SetGDPRRemote(bool emailConsent, Action<bool, string> OnCompletion)
        {
            // this was previously done by coin, but will now be hidden to the user
            bool heads = UnityEngine.Random.Range(0, 2) == 0;
            player.team = heads? PlayerDetails.Team.Lion : PlayerDetails.Team.Wolf;

            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "team", ((int)player.team).ToString() },
                { "newsletter", emailConsent? "1":"0" },
                { "__address__", serverURL+"gdpr.php" },
            };
            pat.Post(data, OnCompletion);
        }

        public void LoginRemote(string username, string password, Action<bool, string> OnCompletion)
        {
            password = Postman.Encrypt(password);
            var data = new Dictionary<string, string>() {
                { "username", username },
                { "password", password },
                { "__address__", serverURL+"login.php" },
            };
            pat.Post(data, (b,s)=>{ if (b) ParseLogin(username, password, s); OnCompletion(b,s); });
        }
        void ParseLogin(string username, string password, string returned)
        {
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
                { "recipient", Postman.Encrypt(recipient) },
                { "__address__", serverURL+"resetup.php" },
            };
            pat.Post(data, OnCompletion);
        }
        public void DeleteAccountRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "__address__", serverURL+"delete.php" },
            };
            pat.Post(data, OnCompletion);
        }

        //////////////////////////////////////////////
        // things that can be saved and posted later

        public void SetDemographicsRemote(int age, int gender, int education)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "age", age.ToString() },
                { "gender", gender.ToString() },
                { "education", education.ToString() },
                { "__address__", serverURL+"demographics.php" },
            };
            Action<bool,string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }

        public void SetDragDirectionLocal(bool reversed)
        {
            player.reverseDrag = reversed;
            SavePlayerDetailsLocal();
        }
        public void SetDragDirectionRemote()
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "reversed", player.reverseDrag? "1":"0" },
                { "__address__", serverURL+"drag.php" },
            };
            Action<bool,string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }

        // This whole framework is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level.LevelDetails, but that is not possible in a build
        public int GetHighScoreLocal(int levelIdx)
        {
            if (!player.highScores.ContainsKey(levelIdx)) {
                return 0;
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
        public void SavePlaythroughRemote(int levelIdx, int score, string matrix, string actions)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "level_index", levelIdx.ToString() },
                { "datetime_ticks", DateTime.Now.Ticks.ToString() },
                { "score", score.ToString() },
                { "matrix", matrix },
                { "actions", actions },
                { "__address__", serverURL+"playthrough.php" },
            };
            Action<bool, string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }


        ///////////////////////////////////////////////////////////////////
        // used to cache results from startup in case player goes into tube
        // TODO: save to file as well in case startup in tube
        public class LeaderboardCache
        {
            public int idx;
            public int median = 0;
            public List<string> names = new List<string>();
            public List<int> scores = new List<int>();
            public LeaderboardCache(int idx) {
                this.idx = idx;
            }
        }
        // only leaderboard does not require a login
        public void CacheLeaderboardsRemote(int n_scores, Action OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "n_scores", n_scores.ToString() }, { "__address__", serverURL+"leaderboards.php" },
            };
            pat.Post(data, (b,s)=>{ if (b) ParseLeaderboards(s); OnCompletion?.Invoke(); });
        }
        private Dictionary<int, LeaderboardCache> cachedLeaderboards;
        private void ParseLeaderboards(string returned)
        {
            cachedLeaderboards = new Dictionary<int, LeaderboardCache>();
            var levels = returned.Split(';');
            foreach (var level in levels)
            {
                var scores = level.Split(',');
                var header = scores[0].Split(':');

                var toCache = new LeaderboardCache(int.Parse(header[0]));
                toCache.median = int.Parse(header[1]);
                for (int i=1; i<scores.Length; i++)
                {
                    var score = scores[i].Split(':');
                    toCache.names.Add(score[0]);
                    toCache.scores.Add(int.Parse(score[1]));
                }
                Assert.IsFalse(cachedLeaderboards.ContainsKey(toCache.idx), "level index already cached");

                cachedLeaderboards[toCache.idx] = toCache;
            }
        }
        public LeaderboardCache GetCachedLeaderboard(int level_idx)
        {
            if (cachedLeaderboards == null) {
                return null;
            }
            if (!cachedLeaderboards.ContainsKey(level_idx))
            {
                cachedLeaderboards[level_idx] = new LeaderboardCache(level_idx);
            }
            return cachedLeaderboards[level_idx];
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
        public void DontAskAgainForLogin()
        {
            player.team = PlayerDetails.Team.NeverAsk;
        }
        public void CreateAccount()
        {
            if (player.team == PlayerDetails.Team.Wolf || player.team == PlayerDetails.Team.Lion) {
                throw new Exception("should not have option to create account");
            }
            // note that this function purposefully does not delete the player in order to keep their highscore info
            player.team = PlayerDetails.Team.Unassigned;
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }
        public void LogOut()
        {
            DeletePlayerDetailsLocal();
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }
        public void OpenPrivacyPolicyInBrowser()
        {
            Application.OpenURL(serverURL+"GDPR_Privacy_Notice.html");
        }
    }
}
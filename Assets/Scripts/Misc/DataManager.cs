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
    // data collection and storage bits of GameManager
    public partial class GameManager : MonoBehaviour
    {
        [Serializable]
        private class PlayerDetails
        {
            public string username = "";
            public string password = "";
            public string email = "";

            public int age = -1;
            public int gender = -1;
            public int education = -1;
            public enum Team { Unassigned=-1, Wolf, Lion, NeverAsk }
            public Team team = Team.Unassigned;

            public bool reverseDrag = true;

            public Dictionary<int, long> highScores = new Dictionary<int, long>();
            public Dictionary<int, long> cachedMedians = new Dictionary<int, long>();
            public Queue<Dictionary<string,string>> unsentPost = new Queue<Dictionary<string,string>>();
            public int unsentCount=0;
        }
        [SerializeField] PlayerDetails player = null;

        public bool LoggedIn { get { return player.team==PlayerDetails.Team.Wolf || player.team==PlayerDetails.Team.Lion; }}
        public bool ConstrainTrophic { get { return false; } }//return player.team != PlayerDetails.Team.Lion; } }
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForRegistration { get { return player.team==PlayerDetails.Team.Unassigned; } }
        public string Username { get { return player.username; } }

        static string playerPath;
        public void InitPlayer() // called by Awake()
        {
            // ugh unity annoying so hard-coded
            playerPath = Application.persistentDataPath+"/player.data";

#if UNITY_EDITOR
            // DeletePlayerDetailsLocal();
#endif
            if (LoadPlayerDetailsLocal() == false) {
                player = new PlayerDetails();
            }
        }
        private void SavePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return;
#endif
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Create(playerPath);
                bf.Serialize(file, player);
                file.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("could not save player: " + e.Message);
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
            // note: this loses the median cache and unsent post
            player = new PlayerDetails();
#if UNITY_WEBGL
            return;
#endif
            try {
                File.Delete(playerPath);
            } catch (Exception e) {
                Debug.LogError("could not delete player: " + e.Message);
            }
        }


        ///////////////////
        // web form things

#if UNITY_EDITOR
        public string ServerURL { get { return "http://127.0.0.1/ecobuilder/"; } }
#else
        public string ServerURL { get { return "https://www.ecobuildergame.org/Beta/"; } }
#endif
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
                { "highscores", ComposeRegistrationScores() },

                { "__address__", ServerURL+"register.php" },
            };
            pat.Post(data, OnCompletion);
        }

        // this is for if the player wants to register after already playing
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
                { "__address__", ServerURL+"gdpr.php" },
            };
            pat.Post(data, OnCompletion);
        }

        public void LoginRemote(string username, string password, Action<bool, string> OnCompletion)
        {
            password = Postman.Encrypt(password);
            var data = new Dictionary<string, string>() {
                { "username", username },
                { "password", password },
                { "__address__", ServerURL+"login.php" },
            };
            pat.Post(data, (b,s)=>{ if (b) ParseLogin(username, password, s); OnCompletion(b,s); });
        }
        private void ParseLogin(string username, string password, string returned)
        {
            player.username = username;
            player.password = password;
            var details = returned.Split(';');
            player.team = (PlayerDetails.Team)int.Parse(details[0]);
            player.reverseDrag = int.Parse(details[1])==1? true:false;

            player.highScores.Clear();
            for (int i=2; i<details.Length; i++)
            {
                string[] level = details[i].Split(':');
                int idx = int.Parse(level[0]);
                long score = long.Parse(level[1]);
                SaveHighScoreLocal(idx, score);
            }
            SavePlayerDetailsLocal();
        }
        public void SendPasswordResetEmail(string recipient, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "recipient", Postman.Encrypt(recipient) },
                { "__address__", ServerURL+"resetup.php" },
            };
            pat.Post(data, OnCompletion);
        }
        public void DeleteAccountRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "__address__", ServerURL+"delete.php" },
            };
            pat.Post(data, (b,s)=>{ OnCompletion(b,s); } );
        }
        public void DontAskAgainForLogin()
        {
            player.team = PlayerDetails.Team.NeverAsk;
            SavePlayerDetailsLocal();
        }
        public void AskAgainForLogin()
        {
            Assert.IsFalse(player.team == PlayerDetails.Team.Wolf || player.team == PlayerDetails.Team.Lion, "should not have option to create account");
            // note that this function purposefully does not delete the player in order to keep their highscore info
            player.team = PlayerDetails.Team.Unassigned;
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
                { "__address__", ServerURL+"demographics.php" },
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
                { "__address__", ServerURL+"drag.php" },
            };
            Action<bool,string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }

        // This whole framework is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level.LevelDetails, but that is not possible in a build
        public long? GetHighScoreLocal(int levelIdx)
        {
            long score;
            if (!player.highScores.TryGetValue(levelIdx, out score)) {
#if UNITY_EDITOR
                return 0;
#else
                return null;
#endif
            }
            return score;
        }
        // returns whether new high score is achieved
        private bool SaveHighScoreLocal(int levelIdx, long score)
        {
            if (GetHighScoreLocal(levelIdx) < score)
            {
                player.highScores[levelIdx] = score;
                SavePlayerDetailsLocal();
                return true;
            }
            return false;
        }
        private void SavePlaythroughRemote(int levelIdx, long score, string matrix, string actions)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "level_index", levelIdx.ToString() },
                { "datetime_ticks", DateTime.Now.Ticks.ToString() },
                { "score", score.ToString() },
                { "matrix", matrix },
                { "actions", actions },
                { "__address__", ServerURL+"playthrough.php" },
            };
            Action<bool, string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }


        ///////////////////////////////////////////////////////////////////
        // used to cache results from startup in case player goes into tube

        public void CacheMediansRemote()
        {
            var data = new Dictionary<string, string>() {
                { "__address__", ServerURL+"medians.php" },
            };
            void CacheMedians(string medians)
            {
                var levels = medians.Split(',');
                foreach (var level in levels)
                {
                    var score = level.Split(':');
                    player.cachedMedians[int.Parse(score[0])] = long.Parse(score[1]);
                }
            }
            pat.Post(data, (b,s)=>{ if (b) CacheMedians(s); });
        }
        public long? GetCachedMedian(int level_idx)
        {
            long median;
            if (player.cachedMedians.TryGetValue(level_idx, out median)) {
                return median;
            } else {
                return null;
            }
        }

        public void GetRankedScoresRemote(int levelIdx, int firstRank, int numRows, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "level_index", levelIdx.ToString() },
                { "first_rank", firstRank.ToString() },
                { "num_scores", numRows.ToString() },
                { "__address__", ServerURL+"leaderboard.php" },
            };
            pat.Post(data, OnCompletion);
        }
        public void GetNearbyRanksRemote(int levelIdx, int rowsAbove, int rowsBelow, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "level_index", levelIdx.ToString() },
            };
            pat.Post(data, OnCompletion);
        }

        private void SavePost(Dictionary<string, string> data)
        {
            player.unsentPost.Enqueue(data);
            player.unsentCount = player.unsentPost.Count;
            SavePlayerDetailsLocal();
        }

        bool sendingUnsent;
        private void SendUnsentPost()
        {
            if (sendingUnsent) {
                return;
            }
            sendingUnsent = true;

            void SendNextIfPossible(bool prevSuccess)
            {
                if (prevSuccess)
                {
                    player.unsentPost.Dequeue();
                    player.unsentCount = player.unsentPost.Count;
                    SavePlayerDetailsLocal();
                    if (player.unsentPost.Count > 0) {
                        pat.Post(player.unsentPost.Peek(), (b,s)=>SendNextIfPossible(b));
                    } else {
                        sendingUnsent = false;
                    }
                }
                else
                {
                    sendingUnsent = false;
                }
            }
            if (player.unsentPost.Count > 0)
            {
                pat.Post(player.unsentPost.Peek(), (b,s)=>SendNextIfPossible(b));
            }
        }


        ////////////////////////////////////
        // purely for testing leaderboards
#if UNITY_EDITOR
        void PopulateDatabaseWithScores()
        {
            for (int i=0; i<26; i++)
            {
                string name = "bob" + (char)('A'+i);
                RegisterLocal(name, "", name+"@bob.co.uk");
                RegisterRemote((b,s)=>print(b+" "+s));
                SetGDPRRemote(true, (b,s)=>print(b+" "+s));
                SetDemographicsRemote(0,1,2);
                for (int j=101; j<=104; j++)
                {
                    SavePlaythroughRemote(j, UnityEngine.Random.Range(100, 100000000), "", "");
                }
            }
        }
#endif
    }
}
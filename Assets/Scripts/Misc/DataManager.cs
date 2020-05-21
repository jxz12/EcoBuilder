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
            public string passwordEncrypted = "";
            public string emailEncrypted = "";

            public int age = -1;
            public int gender = -1;
            public int education = -1;
            public enum Team { Unassigned=-1, Wolf, Lion, NeverAsk }
            public Team team = Team.Unassigned;

            public bool reverseDrag = true;
            public bool levelsUnlockedRegardless = false;

            public Dictionary<int, long> highScores = new Dictionary<int, long>();
            public Dictionary<int, long> cachedMedians = new Dictionary<int, long>();
            // public Queue<Dictionary<string,string>> unsentPost = new Queue<Dictionary<string,string>>();
            // public int unsentCount=0;
        }
        [SerializeField] PlayerDetails player = null;

        public bool LoggedIn { get { return player.team==PlayerDetails.Team.Wolf || player.team==PlayerDetails.Team.Lion; }}
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForRegistration { get { return player.team==PlayerDetails.Team.Unassigned; } }
        public string Username { get { return player.username; } }
// #if UNITY_EDITOR
//         [SerializeField] bool constrainTrophic;
//         public bool ConstrainTrophic { get { return constrainTrophic; } }
// #else
        public bool ConstrainTrophic { get { return player.team != PlayerDetails.Team.Lion; } }
// #endif
        public bool AnyLevelsCompleted { get { return player.highScores.Count > 0; }}
        public bool LevelsUnlockedRegardless { get { return player.levelsUnlockedRegardless; } }

        static string playerPath;
        public void InitPlayer() // called by Awake()
        {
            // ugh unity annoying so hard-coded
            playerPath = Application.persistentDataPath+"/player.data";
            if (LoadPlayerDetailsLocal() == false) {
                player = new PlayerDetails();
            }
        }
        private void SavePlayerDetailsLocal()
        {
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
                Debug.LogWarning($"could not load player: {e.Message}");
                return false;
            }
        }
        private void DeletePlayerDetailsLocal()
        {
            // note: this loses the median cache and unsent post
            player = new PlayerDetails();
            try {
                File.Delete(playerPath);
            } catch (Exception e) {
                Debug.LogError($"could not delete player: {e.Message}");
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
            player.passwordEncrypted = Postman.Encrypt(password);
            player.emailEncrypted = Postman.Encrypt(email);
            SavePlayerDetailsLocal();
        }
        public void RegisterRemote(Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.passwordEncrypted },
                { "email", player.emailEncrypted },

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
                { "password", player.passwordEncrypted },
                { "team", ((int)player.team).ToString() },
                { "newsletter", emailConsent? "1":"0" },
                { "__address__", ServerURL+"gdpr.php" },
            };
            pat.Post(data, OnCompletion);
        }

        public void LoginRemote(string username, string password, Action<bool, string> OnCompletion)
        {
            string passwordEncrypted = Postman.Encrypt(password);
            var data = new Dictionary<string, string>() {
                { "username", username },
                { "password", passwordEncrypted },
                { "__address__", ServerURL+"login.php" },
            };
            pat.Post(data, (b,s)=>{ if (b) ParseLogin(s); OnCompletion(b,s); });

            void ParseLogin(string toParse)
            {
                player.username = username;
                player.passwordEncrypted = passwordEncrypted;
                var details = toParse.Split(';');
                player.team = (PlayerDetails.Team)int.Parse(details[0]);
                player.reverseDrag = int.Parse(details[1])==1? true:false;

                // player.highScores.Clear();
                for (int i=2; i<details.Length; i++)
                {
                    string[] level = details[i].Split(':');
                    int idx = int.Parse(level[0]);
                    long score = long.Parse(level[1]);
                    SaveHighScoreLocal(idx, score);
                }
                SavePlayerDetailsLocal();
            }
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
                { "password", player.passwordEncrypted },
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
            SavePlayerDetailsLocal();
        }
        public void LogOut(Action OnSuccess)
        {
            Assert.IsNotNull(OnSuccess);
            Assert.IsTrue(LoggedIn);
            confirmation.GiveChoice(()=>{ DeletePlayerDetailsLocal(); OnSuccess(); }, "Are you sure you want to log out? Any scores you achieve when not logged in will not be saved to this account.");
        }
        public void DeleteAccount(Action OnSuccess)
        {
            Assert.IsNotNull(OnSuccess);
            Assert.IsTrue(LoggedIn);
            // yes this is spaghetti
            confirmation.GiveChoiceAndWait(()=> DeleteAccountRemote((b,s)=>{ confirmation.FinishWaiting(OnSuccess, b, s); if (b) DeletePlayerDetailsLocal(); }), "Are you sure you want to delete your account? Any high scores you have achieved will be lost.", "Deleting account...");
        }
        public void UnlockAllLevels(Action OnSuccess)
        {
            Assert.IsNotNull(OnSuccess);
            confirmation.GiveChoice(()=>{ player.levelsUnlockedRegardless = !player.levelsUnlockedRegardless; SavePlayerDetailsLocal(); OnSuccess(); }, "Are you sure you want to (un)lock all levels?");
        }

        //////////////////////////////////////////////
        // things that can be saved and posted later

        public void SetDemographicsRemote(int age, int gender, int education)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.passwordEncrypted },
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
                { "password", player.passwordEncrypted },
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
                return null;
            }
            return score;
        }
        // returns whether new high score is achieved
        public bool SaveHighScoreLocal(int levelIdx, long score)
        {
            long? currentScore = GetHighScoreLocal(levelIdx);
            if (currentScore == null || currentScore < score)
            {
                player.highScores[levelIdx] = score;
                SavePlayerDetailsLocal();
                return true;
            }
            return false;
        }
        public void SavePlaythroughRemote(int levelIdx, long score, string matrix, string actions)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.passwordEncrypted },
                { "level_index", levelIdx.ToString() },
                { "datetime_ticks", DateTime.Now.Ticks.ToString() },
                { "score", score.ToString() },
                { "erocs", Postman.Encrypt(score.ToString()) }, // a little protection
                { "matrix", matrix },
                { "actions", actions },
                { "__address__", ServerURL+"playthrough.php" },
            };
            Action<bool, string> OnCompletion = (b,s)=> { if (!b) SavePost(data); };
            pat.Post(data, OnCompletion);
        }


        ///////////////////////////////////////////////////////////////////
        // used to cache results from startup in case player goes into tube

        public void CacheMediansRemote(Action<bool> OnCompletion=null)
        {
            var data = new Dictionary<string, string>() {
                { "__address__", ServerURL+"medians.php" },
            };
            pat.Post(data, CacheMedians);

            void CacheMedians(bool successful, string medians)
            {
                if (successful)
                {
                    var levels = medians.Split(',');
                    foreach (var level in levels)
                    {
                        var score = level.Split(':');
                        player.cachedMedians[int.Parse(score[0])] = long.Parse(score[1]);
                    }
                }
                OnCompletion?.Invoke(successful);
            }
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
        public void GetSingleRankRemote(int levelIdx, long score, Action<bool, string> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "level_index", levelIdx.ToString() },
                { "score" , score.ToString() },
                { "__address__", ServerURL+"rank.php" },
            };
            pat.Post(data, OnCompletion);
        }

        private void SavePost(Dictionary<string, string> data)
        {
            string filename = DateTime.Now.Ticks.ToString();
            using (var file = new StreamWriter($"{Application.persistentDataPath}/{filename}.post"))
            {
                foreach (var kvp in data) {
                    file.WriteLine($"{kvp.Key} {kvp.Value}");
                }
            }
        }

        bool sendingUnsent;
        private void SendUnsentPost()
        {
            if (sendingUnsent) {
                return;
            }
            sendingUnsent = true;

            // TODO: don't send new post until this is cleared, even though order shouldn't matter
            var unsentFiles = Directory.GetFiles(Application.persistentDataPath, "*.post");
            Array.Sort(unsentFiles);
            var unsentQueue = new Queue<Tuple<string, Dictionary<string,string>>>();
            foreach (string filename in unsentFiles)
            {
                var post = new Dictionary<string,string>();
                foreach (var line in File.ReadLines(filename))
                {
                    var kvp = line.Split(' ');
                    post[kvp[0]] = kvp[1];
                }
                if (post.Count > 0) {
                    unsentQueue.Enqueue(Tuple.Create(filename,post));
                }
            }
            if (unsentQueue.Count > 0) {
                pat.Post(unsentQueue.Peek().Item2, (b,s)=>SendNextIfPossible(b));
            }

            // recursive local function to clear queue one by one
            void SendNextIfPossible(bool prevSuccess)
            {
                if (prevSuccess)
                {
                    var sentFilename = unsentQueue.Dequeue().Item1;
                    try {
                        File.Delete(sentFilename);
                    } catch (Exception e) {
                        Debug.LogWarning($"could not delete post: {e.Message}");
                    }
                    if (unsentQueue.Count > 0) {
                        pat.Post(unsentQueue.Peek().Item2, (b,s)=>SendNextIfPossible(b));
                    } else {
                        sendingUnsent = false;
                    }
                }
                else
                {
                    sendingUnsent = false;
                }
            }
        }
    }
}
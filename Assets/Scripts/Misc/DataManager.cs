using System;
using System.Collections;
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
            public bool syncedRemote = false;

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
#if !UNITY_WEBGL
            playerPath = Application.persistentDataPath+"/player.data";
#else
            playerPath = null;
#endif
            DeletePlayerDetailsLocal();
            if (LoadPlayerDetailsLocal() == false)
            {
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
                Debug.LogError("error: " + e.Message);
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
                print("handled exception: " + e.Message);
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
        [SerializeField] UI.Postman pat;
        public void RegisterLocal(string username, string password, string email)
        {
            player.username = username;
            player.password = password;
            player.email = email;
            SavePlayerDetailsLocal();
        }
        public void RegisterRemote(Action<bool> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "email", player.email }
            };
            pat.Post(data, server+"register.php", (b,s)=>OnCompletion(b));
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
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "age", player.age.ToString() },
                { "gender", player.gender.ToString() },
                { "education", player.education.ToString() }
            };
            pat.Post(data, server+"demographics.php", (b,s)=>OnCompletion(b));
            print("TODO: no need to pause, but mark as 'need to send' later if not");
        }
        public void SetTeamLocal(PlayerDetails.Team team)
        {
            player.team = team;
            SavePlayerDetailsLocal();
        }
        public void SetTeamRemote(Action<bool> OnCompletion)
        {
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "team", ((int)player.team).ToString() }
            };
            pat.Post(data, server+"team.php", (b,s)=>OnCompletion(b));
        }
        public void SetDragDirectionLocal(bool reversed)
        {
            player.reverseDrag = reversed;
            SavePlayerDetailsLocal();
        }
        public void SetDragDirectionRemote(Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            var data = new Dictionary<string, string>() {
                { "username", player.username },
                { "password", player.password },
                { "reversed", player.reverseDrag? "1":"0" }
            };
            pat.Post(data, server+"drag.php", (b,s)=>OnCompletion(b));
        }
        public void LoginRemote(string username, string password, Action<bool> OnCompletion)
        {
            var form = new WWWForm();
            var data = new Dictionary<string, string>() {
                { "username", username },
                { "password", password },
            };
            pat.Post(data, server+"login.php", (b,s)=>{ if (b) ParseLogin(username, password, s); OnCompletion(b); });
        }
        void ParseLogin(string username, string password, string returned)
        {
            player.username = username;
            player.password = password;
            var toParse = returned.Split(',');
            player.email = toParse[0];
            player.age = int.Parse(toParse[1]);
            player.gender = int.Parse(toParse[2]);
            player.education = int.Parse(toParse[3]);
            player.team = (PlayerDetails.Team)int.Parse(toParse[4]);
            player.reverseDrag = int.Parse(toParse[5])==1? true:false;
            SavePlayerDetailsLocal();
        }
        public Tuple<int,int,int> GetTop3ScoresRemote(int levelIdx)
        {
            print("TODO: get global scores from server");
            return Tuple.Create(14,12,10);
        }
        public bool SendPasswordResetEmail(string username)
        {
            print("TODO: email page for reset");
            return true;
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
    }
}
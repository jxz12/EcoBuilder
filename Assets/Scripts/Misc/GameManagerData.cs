using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// for web comms
using UnityEngine.Networking;

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
        public class PlayerDetails
        {
            public string username;
            public string password;

            public int age;
            public int gender;
            public int education;
            
            public enum Team { None, Wolf, Lion }
            public Team team = Team.None;
            public bool reverseDrag = true;
            public Dictionary<int, int> highScores;
            public bool dontAskForLogin;
        }
        [SerializeField] PlayerDetails player;
        public PlayerDetails.Team PlayerTeam { get { return player.team; } }

        public bool ConstrainTrophic { get { return player.team != GameManager.PlayerDetails.Team.Lion; } }
        public bool ReverseDragDirection { get { return player.reverseDrag; } }
        public bool AskForLogin { get { return player.team==0 && !player.dontAskForLogin; } }

        static string playerPath;
        private void InitPlayer()
        {
            // ugh unity annoying so hard-coded
            playerPath = Application.persistentDataPath+"/player.data";

            DeletePlayerDetailsLocal();
            bool loaded = LoadPlayerDetailsLocal();
            if (!loaded)
            {
                player.highScores = new Dictionary<int,int>();
                player.highScores[learningLevelPrefabs[0].Details.idx] = 1; // unlock first level
                for (int i=1; i<learningLevelPrefabs.Count; i++)
                {
                    if (player.highScores.ContainsKey(learningLevelPrefabs[i].Details.idx))
                        throw new Exception("two levels with same idx");
                    else
                        player.highScores[learningLevelPrefabs[i].Details.idx] = 1; // unlock first level
                }
                foreach (var researchLevel in researchLevelPrefabs)
                {
                    if (player.highScores.ContainsKey(researchLevel.Details.idx))
                        throw new Exception("two levels with same idx");
                    else
                        player.highScores[researchLevel.Details.idx] = 1; // unlock first level
                }
            }
        }


        private bool SavePlayerDetailsLocal()
        {
#if UNITY_WEBGL
            return true;
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
            DeletePlayerDetailsLocal();
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }

        public bool TryRegister(string username, string password)
        {
            player.username = username;
            player.password = password;
            SavePlayerDetailsLocal();

            StartCoroutine(TryRegisterRemote(username, password));
            return true;
        }
        IEnumerator TryRegisterRemote(string username, string password)
        {
            var form = new WWWForm();
            form.AddField("username", username);
            form.AddField("password", password);
            using (var p = UnityWebRequest.Post("127.0.0.1/ecobuilder/register.php", form))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError || p.isHttpError)
                {
                    print(p.error);
                }
                else
                {
                    print(p.downloadHandler.text);
                }
            }
        }
        public bool TryLogin(string username, string password)
        {
            player.username = username;
            player.password = password;
            SavePlayerDetailsLocal();

            // TODO: fetch high scores from server in a coroutine
            return true;
        }
        public void SetDemographics(int age, int gender, int education)
        {
            player.age = age;
            player.gender = gender;
            player.education = education;
            SavePlayerDetailsLocal();

            // TODO: send these to a server with email as the key
        }
        public void SetTeam(PlayerDetails.Team team)
        {
            player.team = team;
            SavePlayerDetailsLocal();
            // TODO: try sending details again
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
        public void SavePlaythrough(double[,] model, int[,] record)
        {
            var bf = new BinaryFormatter();
            var form = new WWWForm();
            form.AddField("idx", PlayedLevel.Details.idx.ToString());
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, model);
                form.AddBinaryData("model", ms.ToArray());
            }

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, record);
                form.AddBinaryData("record", ms.ToArray());
            }

            print("TODO: send actual data");
            // TODO: cut max size of these things if necessary
            //       if sending fails, store locally and try to send next time
        }
        public void SavePlayedLevelHighScore(int score)
        {
            int idx = PlayedLevel.Details.idx;
            if (score > player.highScores[idx])
                player.highScores[idx] = score;

            if (PlayedLevel.NextLevel != null) // unlock next level
            {
                int nextIdx = PlayedLevel.NextLevel.Details.idx;
                if (player.highScores[nextIdx] < 0)
                    player.highScores[nextIdx] = 0; // TODO: animation here to draw eye towards unlock
            }
            SavePlayerDetailsLocal();
        }


        // This whole structure is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level, but that is not possible in a build
        public int GetPlayerHighScore(int levelIdx)
        {
            return player.highScores[levelIdx];
        }
        public Tuple<int,int,int> GetGlobalTop3Scores(int levelIdx)
        {
            // TODO: get global scores from server
            return Tuple.Create(14,12,10);
        }
        public void SetDragDirection(bool reversed)
        {
            player.reverseDrag = reversed;
            SavePlayerDetailsLocal();
        }
        public bool IsLearningFinished {
            get {
                // check if last learning level has been completed
                if (player.highScores[learningLevelPrefabs[learningLevelPrefabs.Count-1].Details.idx] < 1)
                    return false;
                else
                    return true;
            }
        }
    }
}
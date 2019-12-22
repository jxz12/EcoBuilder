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
            public string email;
            public string password;

            public int age;
            public int gender;
            public int education;
            
            public enum Team { None, Wolf, Lion }
            public Team team = Team.None;
            public bool reverseDrag;
            public List<int> highScores;
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
                player.highScores = new List<int>();
                player.highScores.Add(0); // unlock first level
                for (int i=1; i<levelPrefabs.Count; i++)
                {
                    // player.highScores.Add(-1);
                    player.highScores.Add(0);
                }
            }
            // StartCoroutine(Http());
            // FTP();
        }
        private void FTP()
        {
            // var file = new FileInfo("a.txt");
            // var address = "iclwf-dev01.cc.ic.ac.uk";

            // /data/www/dev/sites/www-ecobuildergame.org
        }



        /////////////////////////////
        // questions at login
        public bool TryLogin(string email, string password)
        {
            player.email = email;
            player.password = password;

            // TODO: fetch high scores from server in a coroutine
            return true;
        }
        public bool TryRegister(string email, string password)
        {
            player.email = email;
            player.password = password;

            // TODO: try to create new account if possible
            return true;
        }
        public void Logout()
        {
            DeletePlayerDetailsLocal();
        }
        public void SetDemographics(int age, int gender, int education)
        {
            player.age = age;
            player.gender = gender;
            player.education = education;
        }
        // TODO: send these to a server with email as the key
        public void SetTeam(PlayerDetails.Team team)
        {
            player.team = team;
            // TODO: try sending details
        }
        public void SetDragDirection(bool reversed)
        {
            player.reverseDrag = reversed;
        }
        public void DontAskAgainForLogin()
        {
            player.dontAskForLogin = true;
        }
        IEnumerator Http()
        {
            // var form = new WWWForm();
            // form.AddField("foo", "barr");
            // using (var p = UnityWebRequest.Post("https://www.ecobuildergame.org/Foo/foo.php", form))
            // {
            //     yield return p.SendWebRequest();
            //     if (p.isNetworkError || p.isHttpError)
            //     {
            //         print(p.error);
            //     }
            //     else
            //     {
            //         print(p.downloadHandler.text);
            //     }
            // }

            // var form = new WWWForm();
            // foreach (var kvp in ICDatabaseLogin.MySQL)
            // {
            //     form.AddField(kvp.Key, kvp.Value);
            // }

            using (var p = UnityWebRequest.Get("https://www.ecobuildergame.org/Foo/bar.php"))
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
        public void ResetSaveData()
        {
            DeletePlayerDetailsLocal();
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }
        public void SavePlayerDetails()
        {
            SavePlayerDetailsLocal();
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

        // This whole structure is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level, but that is not possible in a build
        public int GetLevelHighScore(int levelIdx)
        {
            return player.highScores[levelIdx];
        }

        public void SavePlayedLevelHighScore(int score)
        {
            int idx = PlayedLevel.Details.idx;
            if (score > player.highScores[idx])
                player.highScores[idx] = score;

            int nextIdx = PlayedLevel.Details.idx + 1;
            if (nextIdx >= player.highScores.Count)
                return;

            if (player.highScores[nextIdx] < 0)
                player.highScores[nextIdx] = 0;
                // TODO: animation here to make it look pretty
        }
    }
}
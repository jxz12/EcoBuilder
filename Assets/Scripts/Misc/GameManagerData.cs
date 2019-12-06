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
            // TODO: store this locally and on server
            public string email;
            public string password;

            public int age;
            public int gender;
            public int education;
            
            public int team; // 0 is none (also trophic), 1 is trophic, -1 is unconstrained
            public List<int> highScores;
        }
        [SerializeField] PlayerDetails player;
        public bool ConstrainTrophic { get { return player.team >= 0; } }

        public bool FirstPlay { get; private set; } = true;
        static string playerPath;
        private void InitPlayer()
        {
            // ugh unity annoying
            playerPath = Application.persistentDataPath+"/player.data";

            DeletePlayerDetailsLocal();
            bool loaded = LoadPlayerDetailsLocal();
            if (!loaded)
            {
                player = new PlayerDetails();
                player.team = 0;
                player.highScores = new List<int>();
                player.highScores.Add(0); // unlock first level
                for (int i=1; i<levelPrefabs.Count; i++)
                {
                    player.highScores.Add(-1);
                }
                FirstPlay = true;
            }
            else
            {
                FirstPlay = false;
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


        public bool TryLogin(string email, string password)
        {
            // TODO: fetch high scores from server if possible.
            // StartCoroutine(Http());
            return false;
        }
        public bool TryRegister(string email, string password)
        {
            // TODO: try to create new account if possible
            return false;
        }
        public void Logout()
        {
            // TODO: stop keeping track of playthroughs
        }

        /////////////////////////////
        // questions at first login
        public void SetLogin(string email, string password)
        {
            // TODO: send these to the server to store it 
            //       check if it exists in the database,
        }
        public void SetDemographics(int age, int gender, int education)
        {
            // TODO: save these
        }
        // TODO: send these to a server with email as the key
        public void SetTeam(int team)
        {
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
        public void Quit()
        {
            // TODO: send data if possible? and also on every level finish!
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey("Has Played");
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }
        private bool SavePlayerDetailsLocal()
        {
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
            try
            {
                File.Delete(playerPath);
            }
            catch (Exception e)
            {
                print("no save file to delete: " + e.Message);
            }
        }



    }
}
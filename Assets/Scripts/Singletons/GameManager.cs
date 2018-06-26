using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	private static GameManager gameManager;
	public static GameManager Instance {
        get {
            if (gameManager == null)
                Debug.LogError("No active GameManager");
            return gameManager;
        }
    }
    void Awake()
    {
        if (gameManager == null)
            gameManager = this;
        else if (gameManager != this)
        {
            Debug.LogError("Multiple GameManagers, destroying this one");
            Destroy(gameObject); // this means that there can only ever be one GameObject of this type
        }
        for (int i = 0; i < numFrames; i++)
            timeDeltas.Enqueue(0);
    }




    [SerializeField] int boardSize = 5;
    public int BoardSize { get { return boardSize; } }
    public enum Difficulty { Easy, Medium, Hard };
    public Difficulty difficulty = Difficulty.Medium;

    void Start()
    {
        //LoadScene("Play");
    }
    

	private void LoadScene(string sceneName) {
		StartCoroutine(LoadSceneThenSetActive(sceneName));
	}
	private void UnloadScene(string sceneName) {
		SceneManager.UnloadSceneAsync(sceneName);
	}
    //[SerializeField] UnityEvent startLoadEvent, endLoadEvent;

    //[Serializable] public class FloatEvent : UnityEvent<float>{}
    //[SerializeField] FloatEvent progressEvent;
    private IEnumerator LoadSceneThenSetActive(string sceneName)
    {
        //startLoadEvent.Invoke();
        var loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loading.isDone)
        {
            //progressEvent.Invoke(loading.progress);
            yield return null;
        }
        var scene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(scene);
        //endLoadEvent.Invoke();
    }


    [SerializeField] Text fpsText;
    readonly int numFrames = 10;        
    Queue<float> timeDeltas = new Queue<float>();
    float totalTime = 0;
    private void Update()
    {
        float oldDelta = timeDeltas.Dequeue();
        float newDelta = Time.deltaTime;

        totalTime -= oldDelta;
        totalTime += newDelta;
        fpsText.text = (1 / (totalTime / numFrames)).ToString("0.0");

        timeDeltas.Enqueue(newDelta);
    }
}
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Duck_Spawner : MonoBehaviour
{
    [Header("Spawning Positions")]
    [SerializeField] private float duckHeight = 20f;
    [SerializeField] private float randRange = 0.5f;

    [Header("Timings")]
    [Tooltip("Length of time from click to continuously spawning ducks")]
    [SerializeField] private float continuousDelay = 0.1f;
    private float continuousTimer = 0;

    [SerializeField] private int ducksPerSecond = 30;
    private float timeSinceDuck = 1;

    [Header("Dynamic Queue")]
    [SerializeField] private Dynamic_Queue dynamicQueue = default;

    [Header("Spawn Checking")]
    [SerializeField] private float boxCheckSize = 0.1f;
    [SerializeField] private int maxRandTries = 5;
    [SerializeField] private LayerMask stuckDuckLayerMask = default;

    [Header("Duck Counter UI")]
    [SerializeField] private TextMeshProUGUI duckCounterText = default;
    [SerializeField] private string textFormat = "0000";
    private int ducksSpawnedCount = 0;

    [Space]
    public Duck_Setting[] duckSettings = default;
    public int duckSettingIndex = 2;

    // Start is called before the first frame update
    void Start()
    {
        //Load the duck quality setting index and apply it
        if (PlayerPrefs.HasKey("duckLevel")) duckSettingIndex = PlayerPrefs.GetInt("duckLevel");
        dynamicQueue.SetDuckSettings(duckSettings[duckSettingIndex]);
    }

    // Update is called once per frame
    void Update()
    {
        //If the scene manager is active, reset the time since the last duck was spawned
        if(Scene_Manager.transitionActive)
        {
            timeSinceDuck = 0;
        }

        //When the mouse is first clicker, spawn a duck and start the timer to continuous duck spawning
        if(Input.GetMouseButtonDown(0) && !Pause_Menu.isPaused && !Scene_Manager.transitionActive)
        {
            //Reset the timer for continuous duck spawning
            continuousTimer = 0;

            //Reset the timer for how long since the last duck spawn to be 0
            timeSinceDuck = 0;

            //Spawn a duck where the mouse is
            SpawnDuck(MouseRayPos(), 0, true);
        }

        //If the mouse button is held and the delay from initial click is done, spawn a duck
        if(Input.GetMouseButton(0) && continuousTimer >= continuousDelay && !Pause_Menu.isPaused && !Scene_Manager.transitionActive)
        {
            //Spawn as many ducks at the mouse position as would have spawned based on the ducksPerSecond
            while(timeSinceDuck >= 1f / ducksPerSecond)
            {
                //Spawn a duck where the mouse is
                SpawnDuck(MouseRayPos(), randRange);

                //Reduce the time since the last duck spawned by the time between ideal duck spawns
                timeSinceDuck -= 1f / ducksPerSecond;
            }
            
        }

        //Update the timer to reflect the time since the last frame
        continuousTimer += Time.deltaTime;

        timeSinceDuck += Time.deltaTime;

        //Debug.Log(ducksSpawnedCount);
    }

    //Returns a Vector3 which is the position of the mouse in the game environment
    private Vector3 MouseRayPos()
    {
        //Cast a ray from the mouse position and return the point hit
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            return hit.point;
        }

        return new Vector3(0, duckHeight, 0);
    }

    //Overloaded version of SpawnDuck method that defaults importance to false. This is to not break other uses that don't specify the third parameter.
    private void SpawnDuck(Vector3 spawnPoint, float range)
    {
        SpawnDuck(spawnPoint, range, false);
    }

    //Spawn a duck into the world at the specified point
    private void SpawnDuck(Vector3 spawnPoint, float range, bool important)
    {
        //Change the spawnPoint y value to match the height ducks should spawn at
        spawnPoint.y = duckHeight;

        //Increase the counter for the number of ducks spawned
        ducksSpawnedCount++;

        //Stores the random offset from the spawnpoint
        Vector3 randOffset = new Vector3(0, 0, 0);

        //Repeatedly try different offsets until a valid one is found or until tryCount is past it's maximum
        int tryCount = 0;
        do
        {
            //If the range of randomization is almost nothing, use the offset of 0
            if(range < 0.0001f)
            {
                break;
            }

            //If to many tries have happened, set the offset to 0
            if (tryCount > maxRandTries)
            {
                Debug.LogWarning("Spawn Randomization Failed");
                randOffset.x = 0;
                randOffset.y = 0;
                randOffset.z = 0;
                break;
            }

            //Randomize the offset
            randOffset.x = Random.Range(-range, range);
            randOffset.y = Random.Range(0, range * 2);
            randOffset.z = Random.Range(-range, range);

            //Increase the number of tries
            tryCount++;

            //Condition: Repeat while the offset results in a position inside of an exterior invisible wall
        } while (Physics.CheckBox(spawnPoint + randOffset, new Vector3(boxCheckSize, boxCheckSize, boxCheckSize), Quaternion.identity, stuckDuckLayerMask));

        //Add a duck at the location specified by the spawnPoint with the random offset. Passes the importance value to the dynamic queue
        dynamicQueue.AddDuck(spawnPoint + randOffset, important);

        //Update duck counter text
        duckCounterText.text = ducksSpawnedCount.ToString(textFormat);
    }
}

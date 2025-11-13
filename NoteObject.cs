using System.Collections;
using MusicGame.SelectMusic.Model;
using System.Collections.Generic;
using UnityEngine;

public class NoteObject
{
    public GameObject gameObject;
    public Collider collider;
    public Note note;
    public Vector3 spawnPosition;
    public Vector3 targetPosition;
}
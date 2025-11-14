using UnityEngine;
using Model = MusicGame.SelectMusic.Model;

namespace MusicGame.Game
{
    public class NoteObject
    {
        public GameObject gameObject;
        public Collider collider;
        public Model.Note note;
        public Vector3 spawnPosition;
        public Vector3 targetPosition;
    }
}
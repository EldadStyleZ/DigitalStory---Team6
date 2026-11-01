using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A group of minions that live and wake together, some of them perhaps hidden.</summary>
    [Serializable]
    public sealed class GangType
    {
        [SerializeField, Tooltip("A name for finding it in the list.")]
        private string _name;

        [SerializeField, Tooltip("The name of each member. Each must match a minion in the population.")]
        private string[] _members;

        [SerializeField, Min(0), Tooltip("How many of the last members stay hidden until the Piper is close. All of them makes an ambush from nowhere.")]
        private int _hiddenCount;

        [SerializeField, Range(0f, 1f), Tooltip("How deep it starts to appear: 0 is the entrance and 1 the furthest cell.")]
        private float _firstDepth;

        [SerializeField, Range(0f, 1f), Tooltip("How deep it stops appearing.")]
        private float _lastDepth;

        public GangType(string name, string[] members, int hiddenCount, float firstDepth, float lastDepth)
        {
            _name = name;
            _members = members;
            _hiddenCount = hiddenCount;
            _firstDepth = firstDepth;
            _lastDepth = lastDepth;
        }

        public string Name => _name;
        public IReadOnlyList<string> Members => _members ?? Array.Empty<string>();
        public int HiddenCount => _hiddenCount;
        public float FirstDepth => _firstDepth;
        public float LastDepth => _lastDepth;
    }
}

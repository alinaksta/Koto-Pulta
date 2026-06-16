using Itemworks.Core;
using System;
using UnityEngine;

namespace Game.Items.Properties
{
    [Serializable]
    public class SpriteProperty : ItemProperty
    {
        public Sprite Sprite;
    }

    [Serializable]
    public class HandSpriteProperty : ItemProperty
    {
        public Sprite HandSprite;
    }

    [Serializable]
    public class ThrowableProperty : ItemProperty
    {
        public float HoldTime = 0.8f;
        public float ForwardForce = 6f;
    }
}
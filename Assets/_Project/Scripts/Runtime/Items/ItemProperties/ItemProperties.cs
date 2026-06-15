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
}
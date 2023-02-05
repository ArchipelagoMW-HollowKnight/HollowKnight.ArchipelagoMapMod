using System;
using Newtonsoft.Json;
using UnityEngine;

namespace APMapMod.Map;

[Serializable]
public class CoOpPlayer
{
    public string uuid;

    public float r, g, b, x, y;


    [JsonIgnore]
    public Color Color {
    get => new(r , g, b);
    init
    {
        r = value.r;
        g = value.g; 
        b = value.g; 
                
    }}

    [JsonIgnore]
    public Vector2 Pos
    {
        get => new(x, y);
        init
        {
            x = value.x;
            y = value.y;
        }
    }
}
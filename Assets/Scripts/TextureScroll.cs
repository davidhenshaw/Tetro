using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureScroll : MonoBehaviour
{
    Material _material;
    Image image;
    public float xScroll;
    public float yScroll;

    // Start is called before the first frame update
    void Awake()
    {
        _material = GetComponent<SpriteRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        _material.mainTextureOffset = new Vector2(Time.time * xScroll, Time.time * yScroll);
    }
}

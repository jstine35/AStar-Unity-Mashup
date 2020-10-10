using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

public class TextWavePop : MonoBehaviour
{
    public GameObject textCharPrefab;

    GameObject[] chars = new GameObject[256];


    // Start is called before the first frame update
    void Start()
    {
        float curx = 0;
        for(int i=0; i<8; ++i) {        
            chars[i] = Instantiate(textCharPrefab, Vector3.right * curx, Quaternion.identity);

            var tmp = chars[i].GetComponent<TextMeshPro>();

            char ch = (char)('a' + i);
            tmp.text = $"{ch}";
            
            // This can't work because TMPro's string assignment is deferred and the state of
            // the object is invalid for 1+ frames from the time text is assigned (UGGGH). horrible.
            var glyph = tmp.textInfo.characterInfo[0].textElement.glyph;
            curx += glyph.glyphRect.width;
        }
    }

    float curpos = 0;

    // Update is called once per frame
    void Update()
    {
        curpos += Time.deltaTime;
        for(int i=0; i<8; ++i) {
            chars[i].GetComponent<TextMeshPro>().fontSize = 28 + (Mathf.Sin(curpos+i) * 4);
        }
    }
}

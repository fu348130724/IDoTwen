using UnityEngine;
using System.Collections;
using System;

using UnityEngine.EventSystems;
using System.IO;
public class Test : MonoBehaviour
{

    void Start()
    {


        //FF0000FF  red
        transform.GetComponent<MeshRenderer>().material.color = StringToColor("FFFFFFFF");


        //UInt16 C = #3A5FCD; //16进制颜色值
        //byte R, G, B;   //8位RGB值
        //R = (byte)(C >> 10);             //取出高位R的分量
        //G = (byte)((C >> 5) & 0x1f);  //取出高位G的分量
        //B = (byte)(C & 0x1f);             //取出高位B的分量
        ////Color c = Color.HSVToRGB(255, R, G, B);  //这个是16位组合5位R、5位G、5位B
        ////Color c1 = Color.FromArgb(255, 0xff, 0, 0); //#ff0000  这个是24位组合8位R、8位G、8位B

        //Color c1 = Color.HSVToRGB(R, G, B);

       // string colorStr = System.Drawing.ColorTranslator.ToHtml(Color.Red);  

        Debug.Log(CutString("##45699"));
    }



    //public static System.Drawing.Color colorHx16toRGB(string strHxColor)
    //{
    //    return System.Drawing.Color.FromArgb(System.Int32.Parse(strHxColor.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier), System.Int32.Parse(strHxColor.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier), System.Int32.Parse(strHxColor.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
    //}  



    public Color StringToColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr))
        {
            return new Color();
        }
        int colorInt = int.Parse(colorStr, System.Globalization.NumberStyles.AllowHexSpecifier);
        return IntToColor(colorInt);
    }

    
    public static Color IntToColor(int colorInt)
    {
        float basenum = 255;

        int b = 0xFF & colorInt;
        int g = 0xFF00 & colorInt;
        g >>= 8;
        int r = 0xFF0000 & colorInt;
        r >>= 16;
        return new Color((float)r / basenum, (float)g / basenum, (float)b / basenum, 1);

    }


    public string CutString(string str)
    {

        return str.Substring(1, str.Length - 1);
    }
}
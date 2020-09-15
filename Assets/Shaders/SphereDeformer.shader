Shader "SphereDeformer"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Depth ("Depth", Range(-1,1)) = 1
        _High ("High", Range(0,0.5)) = 0.5
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200

        CGPROGRAM
        #pragma surface surf OffModel alpha vertex:vert

        sampler2D _MainTex;
        half _Depth;
        half _High;

    half4 LightingOffModel (SurfaceOutput s, half3 lightDir, half atten)
    {
        half4 c;
        c.rgb = s.Albedo;
        c.a = s.Alpha;
        return c;
    }

    struct Input
    {
        float2 uv_MainTex;
    };

    void vert (inout appdata_full v,out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input,o);

        if(v.color.r<0.1)
        {
            v.vertex.y -=_Depth*2.8;
            v.vertex.x -=_High*5.5;
        }
    }
}

void surf (Input IN, inout SurfaceOutput o)
{
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a;
}

ENDCG
}


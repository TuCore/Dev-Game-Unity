Shader "Custom/RollingDoor" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _EmissionMap ("Emission", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        
        [Header(Door Settings)]
        _DoorOpenAmount ("Door Open Amount", Range(0, 1)) = 0.0
        _DoorWorldXMin ("Door World X Min", Float) = 48.0
        _DoorWorldXMax ("Door World X Max", Float) = 60.0
        _DoorWorldYMin ("Door World Y Min", Float) = 0.0
        _DoorWorldYMax ("Door World Y Max", Float) = 15.0
    }
    SubShader {
        Tags { "RenderType"="AlphaTest" "Queue"="AlphaTest" }
        LOD 200
        Cull Off // Tắt cull để nếu nhìn từ trong ra ngoài vẫn thấy tường

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        sampler2D _EmissionMap;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        fixed4 _Color;
        fixed4 _EmissionColor;
        
        float _DoorOpenAmount;
        float _DoorWorldXMin;
        float _DoorWorldXMax;
        float _DoorWorldYMin;
        float _DoorWorldYMax;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Kiểm tra xem pixel hiện tại có nằm trong khu vực của cánh cửa không (theo trục X và Y thế giới)
            bool inDoorX = (IN.worldPos.x >= _DoorWorldXMin && IN.worldPos.x <= _DoorWorldXMax);
            
            // Tính toán mép dưới của cửa cuốn đang cuộn lên
            float currentDoorBottom = lerp(_DoorWorldYMin, _DoorWorldYMax, _DoorOpenAmount);

            // Nếu pixel nằm trong cửa và thấp hơn mép dưới cửa cuốn -> Xóa pixel đó (tàng hình)
            if (inDoorX && IN.worldPos.y < currentDoorBottom && IN.worldPos.y >= _DoorWorldYMin) {
                clip(-1);
            }

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 metal = tex2D (_MetallicGlossMap, IN.uv_MainTex);
            
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Metallic = metal.r;
            o.Smoothness = metal.a * _Glossiness;
            o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionColor.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

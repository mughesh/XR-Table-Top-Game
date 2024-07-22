// Copyright(c) Meta Platforms, Inc. and affiliates.
// All rights reserved.
//
// Licensed under the Oculus SDK License Agreement (the "License");
// you may not use the Oculus SDK except in compliance with the License,
// which is provided at the time of installation or download, or which
// otherwise accompanies this software in either electronic or hard copy form.
//
// You may obtain a copy of the License at
//
// https://developer.oculus.com/licenses/oculussdk/
//
// Unless required by applicable law or agreed to in writing, the Oculus SDK
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
Shader "Meta/MRUK/MixedReality/InvisibleOccluder" {
  Properties {
  }
  SubShader {
    Tags{"RenderType" = "Transparent"}
    LOD 100
    Cull off
    ZWrite On
    ZTest Less
    Blend Zero One, Zero One
    Pass {
      CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
      };

      v2f vert(appdata v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        return o;
      }

      fixed4 frag(v2f i) : SV_Target {
        return float4(0,0,0,0);
      }
      ENDCG
    }
  }
}

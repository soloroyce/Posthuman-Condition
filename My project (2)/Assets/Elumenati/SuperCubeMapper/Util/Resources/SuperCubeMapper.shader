// warning this shader needs clement to reduce instructions...
// use keywords to reduce texture samples
// rewrite TestCoordinateInside01Fuzzy

Shader "Hidden/Elumenati/SuperCubeMapper" {
    Properties {
        _MainTex0 ("Texture0", 2D) = "black" {}
        _MainTex1 ("Texture1", 2D) = "black" {}
        _MainTex2 ("Texture2", 2D) = "black" {}
        _MainTex3 ("Texture3", 2D) = "black" {}
        _MainTex4 ("Texture4", 2D) = "black" {}
        _MainTex5 ("Texture5", 2D) = "black" {}
        
    }
    SubShader {
        // No culling or depth
    //    Cull Off ZWrite ON ZTest LEQUAL
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
           #pragma multi_compile EQUICUBE EQUICUBE90 EQUIRECTANGULAR EQUIRECTANGULAR90 FISHEYE FISHEYE90 OMNITY OMNITY90
           
           #pragma multi_compile MONO STEREO
           #pragma multi_compile FIX_ALPHA_ENABLED FIX_ALPHA_DISABLED
           #pragma multi_compile FULLDOME CROP
           
#if EQUICUBE
    #define D_EQUICUBE 1 
#endif

#if EQUICUBE90
    #define D_EQUICUBE90 1 
#endif
#if FISHEYE
    #define D_FISHEYE 1 
#endif

#if FISHEYE90
    #define D_FISHEYE90 1 
#endif

#if OMNITY
    #define D_OMNITY 1 
#endif

#if OMNITY90
    #define D_OMNITY90 1 
#endif

#if EQUIRECTANGULAR
    #define D_EQUIRECTANGULAR 1 
#endif
#if EQUIRECTANGULAR90
    #define D_EQUIRECTANGULAR90 1 
#endif

#if FULLDOME
    #define D_FULLDOME 1 
#endif

#if CROP
    #define D_CROP 1 
#endif


#if FIX_ALPHA_ENABLED
    #define D_FIX_ALPHA 1 
#endif


            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

			      uniform half4x4 _CamMatrixArray[6];
            
#if STEREO 
            uniform half4 _OffsetScale = half4(0,0,1,1);
#endif

#if CROP
   uniform float _fisheye_offset = 1;
#endif

#if D_FISHEYE | D_FISHEYE90 | D_OMNITY | D_OMNITY90
   uniform half4x4  _projector_rotation ;
#endif

#if D_OMNITY | D_OMNITY90
  uniform float3 _domexyz = float3(0,0,0);
  uniform float3 _projectorxyz = float3(0,0,0);
#endif



            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
#if D_EQUICUBE 
                float2 u3v2 : TEXCOORD0;
#endif
#if  D_EQUICUBE90 |D_EQUIRECTANGULAR |D_EQUIRECTANGULAR90 | D_FISHEYE | D_FISHEYE90| D_OMNITY | D_OMNITY90
                float2 uv : TEXCOORD0;
#endif
            };
// 130 ins
// 93 ins
// 95 ins
            v2f vert (appdata v) {
                v2f o;
#if D_EQUICUBE 
                o.vertex = float4(v.uv.xy * 2-1,1,1);
                o.u3v2 = float2(v.uv.x * 3,v.uv.y * 2);
#elif D_EQUICUBE90 
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2( v.uv.x*3 ,v.uv.y*2 );
#elif D_EQUIRECTANGULAR |D_EQUIRECTANGULAR90  | D_FISHEYE | D_FISHEYE90| D_OMNITY | D_OMNITY90
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2( 1-v.uv.x , 1-v.uv.y  );
#endif


#if STEREO 
// used to make stereo pairs be over under or side by side.
// equicube needs to be rotated.
  #if D_EQUICUBE | D_EQUICUBE90 
                o.vertex.xy = o.vertex.yx;	// ROTATION FOR EQUICUBE SET USING THE STEREO FLAG && EQUICUBE90/EQUICUBE
  #endif
                o.vertex.xy = _OffsetScale.xy  +o.vertex.xy * _OffsetScale.zw; 
#endif
                return o;
            }

            sampler2D _MainTex0;
            sampler2D _MainTex1;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            sampler2D _MainTex4;
            sampler2D _MainTex5;

            uniform float _borderb_fov_over_90_minus_one =  (100.0-90.0)/90.0;

            bool TestCoordinateInside01FAST(half4 inProj, half2 iny) {
                return  inProj.w >= 0.0 &&  all(half4(iny.xy, 1 - iny.xy) >= 0);
            }

         

         //better logic functions.... testing

     float2 when_eq(float2 x, float2 y) {
  return 1.0 - abs(sign(x - y));
}

float2 when_neq(float2 x, float2 y) {
  return abs(sign(x - y));
}

float2 when_gt(float2 x, float2 y) {
  return max(sign(x - y), 0.0);
}

float2 when_lt(float2 x, float2 y) {
  return max(sign(y - x), 0.0);
}

float2 when_ge(float2 x, float2 y) {
  return 1.0 - when_lt(x, y);
}

float2 when_le(float2 x, float2 y) {
  return 1.0 - when_gt(x, y);
}
float2 and(float2 a, float2 b) {
  return a * b;
}

float2 or(float2 a, float2 b) {
  return min(a + b, 1.0);
}

float2 xor(float2 a, float2 b) {
  return (a + b) % 2.0;
}

float2 not(float2 a) {
  return 1.0 - a;
}

// logic functions end

      float EdgeBlend1D(float x){
                float val =1;
                if(x <_borderb_fov_over_90_minus_one){
                    val =x/_borderb_fov_over_90_minus_one;
                } 
                if(x > 1-_borderb_fov_over_90_minus_one){
                    val = (1-x)/_borderb_fov_over_90_minus_one;
                }
                val = clamp(val,0,1);
                return val;
            }
                float TestCoordinateInside01Fuzzy(half4 inProj, half2 iny) {
                  return  ceil(inProj.w) * EdgeBlend1D(iny.x) * EdgeBlend1D(iny.y);
            }
            fixed4 LookUpOrZero(sampler2D tex, half4x4 mat, float4 ray){
#if D_EQUIRECTANGULAR90
                float4 uv = mul(mat,ray);
                uv.xy /= uv.w;

float2 value = 1;
value *= uv.xy >=0;
value *= uv.xy <=1;
value *= uv.w > 0;
                return  tex2D(tex,uv.xy) *value.x * value.y  ;
#else
                float4 uv = mul(mat,ray);
                uv.xy /= uv.w;
                
                float4 color =tex2D(tex,uv.xy);
#if D_FIX_ALPHA             
                color.a = 1;
#endif
                return  color * TestCoordinateInside01Fuzzy(uv,uv.xy);
#endif
            }



#if D_EQUICUBE
            float4 UV_23_to_ray4(float2 uvu3v2){
                float4 ray = float4(.5- frac(uvu3v2), .5,1);
                float2 flip = float2(1,-1);
                return uvu3v2.y>=1 ?       
                ((uvu3v2.x >=2) ? ray.yzxw * flip.xxyx : ((uvu3v2.x >=1) ? ray.yxzw * flip.xyyx:ray.yzxw * flip.xyxx))
                :(uvu3v2.x >=2 ? ray.zyxw :(uvu3v2.x >=1 ? ray * flip.yxxx:ray.zyxw * flip.yxyx));
            }
#elif D_EQUICUBE90
            float4 LookUp(float2 uv2,float2 uv){
                return  (uv.y>1) *( (uv.x<1 ) * tex2D(_MainTex0,uv2 ) +
                                   (uv.x>=2) * tex2D(_MainTex2,uv2 ) +
                                    (uv.x>=1) * (uv.x<2) * tex2D(_MainTex1,uv2 ) )+
                        (uv.y<=1)*(
                        (uv.x<1 ) * tex2D(_MainTex3,uv2 )+
                        (uv.x>=2) * tex2D(_MainTex5,uv2 )+
                        (uv.x>=1) * (uv.x<2) * tex2D(_MainTex4,uv2 ));
            }
#endif




#if D_OMNITY90 | D_OMNITY 


float RaySphereIntersect(float3 sphereCenter, float3 rayOrigin, float3 ray){
   float3 off=  sphereCenter-rayOrigin;
   float tca = dot(ray,off);
   float radiusSquared = 1;
   
   
   float d2 = dot(off,off)-tca*tca;
   float thc =sqrt( 1-d2);
   
 float checkRangeSUB=  tca-thc;
 float checkRangeADD=  tca+thc;
 
 float minR = min(checkRangeSUB,checkRangeADD);
 float maxR = max(checkRangeSUB,checkRangeADD);
 float A = minR;
 
 if(A<0){
     A = maxR;
 }
 
 if(A<0){
    A=0;
 }
 
 if(d2> 1){
 A=-1;
 }
return A; 
}
#endif



#if D_OMNITY90 | D_OMNITY | D_FISHEYE | D_FISHEYE90

       float4 UV_01_toRay4_FISHEYE(float2 uv01){
                float2 xyInput = ( uv01- float2(.5,.5))*2;
                
                float2 resXY =_ScreenParams.xy;
                #if STEREO
                    resXY.y *= .5;
                #endif

#if D_OMNITY90 | D_OMNITY              
                
    #if D_FULLDOME
              //if(_ScreenParams.x>_ScreenParams.y){                      // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
                    xyInput.x *= - resXY.x/resXY.y;
         

              // }else{                                                   // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
                //    xyInput.y *= resXY.y/resXY.x;       // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
              // }                                                        // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
    #elif D_CROP
                //   if(_ScreenParams.x<_ScreenParams.y){                     // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
                //              xyInput.x *= resXY.x/resXY.y; // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
                //       }else{                                               // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
                        xyInput.x *=-1;

                        xyInput.y *= resXY.y/resXY.x;
                        xyInput.y += _fisheye_offset * (resXY.y/resXY.x -1);
                //     }                                                      // uncomment this stuff if you use a screen that is taller than wide.  I ignored this case because 99% of the time this is false
    #endif

#else
               xyInput.x *= - resXY.x/resXY.y;
#endif



                float2 xyInputSquared = xyInput*xyInput;
                float R = 1- sqrt(xyInputSquared.x+xyInputSquared.y);
                
                
                
                // rotate by projector rotation....
                float3 rotatedProjectorRay = mul( _projector_rotation,float4(xyInput.x,-xyInput.y,sqrt(R),1)).xyz;
                
                #if D_OMNITY90 | D_OMNITY
                    float T = RaySphereIntersect(_domexyz,_projectorxyz,rotatedProjectorRay);
                    return float4(rotatedProjectorRay* T+_projectorxyz,1);
                #else
                    return float4(rotatedProjectorRay,1);
                #endif
                
            }
#endif
#if D_EQUIRECTANGULAR | D_EQUIRECTANGULAR90
#define PI  (3.141593) 
            float4 UV_01_toRay4(float2 uv01){
                float theta  = (uv01.x * PI * 2.0);
                float phi    = (uv01.y * PI);
                float sinPHI = sin(phi);
                float cosPHI = cos(phi);
                float cosTHETA = cos(theta);
                float sinTHETA = sin(theta);
                return float4(
                 sinPHI * sinTHETA,
                    cosPHI,
                    - sinPHI * cosTHETA     ,1);
            }
#endif
            fixed4 frag (v2f i) : SV_Target {
#if D_EQUICUBE
                float4 normal = UV_23_to_ray4(i.u3v2);
#elif D_EQUICUBE90
               float2 uv2 = fmod(i.uv,1);
#elif D_EQUIRECTANGULAR |D_EQUIRECTANGULAR90
                float4 normal  = UV_01_toRay4(i.uv.xy);
#elif D_FISHEYE90 |D_FISHEYE | D_OMNITY | D_OMNITY90
// UV to fisheye normal code here
                float4 normal  = UV_01_toRay4_FISHEYE(i.uv.xy);
#endif


#if D_EQUICUBE90
                return LookUp(uv2,i.uv);
#else
   float4 val = LookUpOrZero(_MainTex0,_CamMatrixArray[0],normal)+
                LookUpOrZero(_MainTex1,_CamMatrixArray[1],normal)+
                LookUpOrZero(_MainTex2,_CamMatrixArray[2],normal)+
                LookUpOrZero(_MainTex3,_CamMatrixArray[3],normal)+
                LookUpOrZero(_MainTex4,_CamMatrixArray[4],normal)+
                LookUpOrZero(_MainTex5,_CamMatrixArray[5],normal);
                if(val.w>1){
                   val.xyzw /= val.w;
                }
                return val;
#endif  
            }
            ENDCG
        }
    }
}

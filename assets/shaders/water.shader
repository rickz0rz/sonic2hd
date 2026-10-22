#version 330

// Copyright (c) Ted John. 2014. All rights reserved.
//
// This source is subject to a License.
// Please see the license.txt file for more information.
// All other rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

@uniform:
    const float PI = 3.1415926535897932384626433832795;

    uniform mat4 ProjectionMatrix;
    uniform sampler2D InputTexture;
	uniform sampler2D WaveTexture;
    uniform float InputHueTarget;
    uniform float InputHueAmount;
    uniform float InputSaturationChange;
    uniform float InputLuminosityChange;
    uniform float InputWavePhase;
    uniform float InputNumWaves;
    uniform float InputWaveSize;
    uniform float iGlobalTime;
    uniform float InputPositionX, InputPositionY;
    uniform float InputWaterLevel;// this is without the waves and the reflections
#define TAU 6.28318530718
#define MAX_ITER 5
#define F length(.5-fract(k.xyw*=mat3(-2,-1,2, 3,-2,1, 1,2,2)*
@vertex_input:
    layout (location = 0) in vec2 VertexPosition;
    layout (location = 1) in vec2 VertexTextureMapping;

@vertex_output:
    out vec2 FragmentTextureMapping;
    
@fragment_input:
    in vec2 FragmentTextureMapping;
    
@fragment_output:
    layout (location = 0) out vec4 OutputColour;

@vertex:
    void main()
    {
        FragmentTextureMapping = VertexTextureMapping;
        gl_Position = ProjectionMatrix * vec4(VertexPosition, 0.0, 1.0);
    }

@fragment:

    float wrap(in float x, in float max, in float min)
    {
        float range = max - min;
        while (x < min)
            x += range;
        while (x > max)
            x -= range;
        return x;
    }

    float goTowardsWrap(in float x, in float dest, in float change, in float minValue, in float maxValue)
    {
        x = wrap(x, maxValue, minValue);
        dest = wrap(dest, maxValue, minValue);

        if (x == dest)
            return x;
            
        if (x < dest) {
            float leftDelta = (x - minValue) + (maxValue - dest);
            float rightDelta = dest - x;
            if (leftDelta < rightDelta)
                return wrap(x - (leftDelta * change), maxValue, minValue);
            else
                return x + (rightDelta * change);
        } else {
            float leftDelta = x - dest;
            float rightDelta = (maxValue - x) + (dest - minValue);
            if (leftDelta < rightDelta)
                return x - (leftDelta * change);
            else
                return wrap(x + (rightDelta * change), maxValue, minValue);
        }
    }
    vec2 hash2(vec2 p ) {
       return fract(sin(vec2(dot(p, vec2(123.4, 748.6)), dot(p, vec2(547.3, 659.3))))*5232.85324);   
    }
    float hash(vec2 p) {
      return fract(sin(dot(p, vec2(43.232, 75.876)))*4526.3257);   
    }

    float voronoi(vec2 p) {
        vec2 n = floor(p);
        vec2 f = fract(p);
        float md = 5.0;
        vec2 m = vec2(0.0);
        for (int i = -1;i<=1;i++) {
            for (int j = -1;j<=1;j++) {
                vec2 g = vec2(i, j);
                vec2 o = hash2(n+g);
                o = 0.5+0.5*sin(iGlobalTime+5.038*o);
                vec2 r = g + o - f;
                float d = dot(r, r);
                if (d<md) {
                  md = d;
                  m = n+g+o;
                }
            }
        }
        return md;
    }

    float ov(vec2 p) {
        float v = 0.0;
        float a = 0.4;
        for (int i = 0;i<3;i++) {
            v+= voronoi(p)*a;
            p*=2.0;
            a*=0.5;
        }
        return v;
    }

    vec4 causticsImage(vec2 fragCoord) 
    {
        vec2 iResolution = vec2( 19/5, 10/5);
        float time = iGlobalTime * .5 + 23.0;
        // uv should be the 0-1 uv of texture...
        vec2 uv = fragCoord.xy / iResolution.xy;
    
        #ifdef SHOW_TILING
            vec2 p = mod(uv*TAU*2.0, TAU)-250.0;
        #else
            vec2 p = mod(uv*TAU, TAU)-250.0;
        #endif
        vec2 i = vec2(p);
        float c = 1.0;
        float inten = .005;

        for (int n = 0; n < MAX_ITER; n++) 
        {
            float t = time * (1.0 - (3.5 / float(n+1)));
            i = p + vec2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
            c += 1.0/length(vec2(p.x / (sin(i.x+t)/inten),p.y / (cos(i.y+t)/inten)));
        }
        c /= float(MAX_ITER);
        c = 1.17-pow(c, 1.4);
        vec3 colour = vec3(pow(abs(c), 8.0));
        colour = clamp(colour + vec3(0.0, 0.35, 0.5), 0.0, 1.0);
    

        #ifdef SHOW_TILING
        // Flash tile borders...
        vec2 pixel = 2.0 / iResolution.xy;
        uv *= 2.0;

        float f = floor(mod(iGlobalTime*.5, 2.0)); 	// Flash value.
        vec2 first = step(pixel, uv) * f;		   	// Rule out first screen pixels and flash.
        uv  = step(fract(uv), pixel);				// Add one line of pixels per tile.
        colour = mix(colour, vec3(1.0, 1.0, 0.0), (uv.x + uv.y) * first.x * first.y); // Yellow line
    
        #endif
        return vec4(colour, 1.0);
    }

	vec4 mainImageTransform(vec2 p)//new caustics2.0
	{
		vec4 k;
		k=vec4(2017.,21.,9.,iGlobalTime*0.4)*.5;
		k.xy = p*(sin(iGlobalTime*0.0004)+2.)/2e2;
		k = pow(min(min(F.5)),F.4))),F.3))), 7.)*25.+vec4(0,.35,.5,1);
		return k;
	}
	vec3 overlay(vec3 a, vec3 b)
	{
	 float midgray=0.5;
	  float midA = (a.x+a.y+a.z)/3;
     
	  vec3 c;
	  if(midA<midgray) c.x=2*a.x*b.x; else c.x=(1-2*(1-a.x)*(1-b.x));
	  if(midA<midgray) c.y=2*a.y*b.y; else c.y=(1-2*(1-a.y)*(1-b.y));
	  if(midA<midgray) c.z=2*a.z*b.z; else c.z=(1-2*(1-a.z)*(1-b.z));
	  return c;
	}
    void main()
    {
		float waveTextureWidth = 256.0;
		float waveTextureHeight = 106.0*1;
        float xoffset =0* 0.5*sin(InputWavePhase + (FragmentTextureMapping.y * PI * 2 * InputNumWaves)) * (InputWaveSize / 1920.0);
		
        // Get texture colour

        float waterBlendFactor = 0.7f;//cpz settings
		
      	vec2 uv= FragmentTextureMapping.xy;
		float uvMultiplier = 25.0/2.0;
        float X = uv.x*uvMultiplier+iGlobalTime;
        float Y = uv.y*uvMultiplier+iGlobalTime;
        uv.y =uv.y+ cos(X+Y)*0.0031*cos(Y);
        uv.x =uv.x+ sin(X-Y)*0.0031*sin(Y);
		
        vec4 texel = texture(InputTexture, uv);//FragmentTextureMapping + vec2(xoffset, 0));
		


		float waveTexelCoordinatesY=(1+0.065 - (InputWaterLevel+FragmentTextureMapping.y)) * 1080.0 / waveTextureHeight;
		
        vec4 waveTexel = texture(WaveTexture, vec2((InputPositionX + FragmentTextureMapping.x * 1920.0) / waveTextureWidth, waveTexelCoordinatesY));

		float topOfWaterHeight = 0;
        float waveBlendHeight = 0;
        vec4 transform;

		//CPZ
        {
            waterBlendFactor = 0.5f;
            float waveHeight = 20 / 1080.0f;
            waveBlendHeight = sin(20 * (InputPositionX / 1920.0f + FragmentTextureMapping.x));//
            topOfWaterHeight = -waveHeight + waveBlendHeight *waveHeight *sin(iGlobalTime * 4);
        }
		vec4 k =mainImageTransform((uv+0*FragmentTextureMapping.xy + vec2(InputPositionX / 1920.0f, InputPositionY / 1080.0f)) *1000);
        vec3 b=k.xyz;//caustics
	  	b=vec3(b.x+b.y+b.z)/10;
		texel.xyz=texel.xyz+b;
      	vec4 mixColorA = vec4(0, 0, 64/255.0, 1);//* waterBlendFactor + (1 - waterBlendFactor) * texel;
		vec4 mixColorB = vec4(255/255.0, 0, 255/255.0, 1)* waterBlendFactor + (1 - waterBlendFactor) * texel;
		vec4 mixColorC = vec4(255/255.0, 162/255.0, 223/255.0, 1)* waterBlendFactor + (1 - waterBlendFactor) * texel;
		float mixFactor=(texel.x+texel.y+texel.z)/3;
		
		if(mixFactor<0.6)
		{
			mixFactor=mixFactor/0.6;
			transform =(1 - mixFactor) * mixColorA +  (mixFactor) * mixColorB;
		}
		else
		{
			mixFactor = mixFactor - 0.6;
			mixFactor = mixFactor/0.4;
			transform =(1 - mixFactor) * mixColorB +  (mixFactor) * mixColorC;
		}
        //transform.xyz = transform.xyz * 1 + vec3(1,1,1) * ov(FragmentTextureMapping.xy*10);
      //  transform.xyz = transform.xyz * 1 + 0.25f * vec3(1, 1, 1) * causticsImage((FragmentTextureMapping.xy + vec2(InputPositionX / 1920.0f, InputPositionY / 1080.0f)) * 10).x;
	 
	  vec3 a=transform.xyz;//color
	
	  //caustics to gradient
	   /* mixFactor=b.x;
		mixColorA = vec4(0, 0, 64/255.0, 1);
		mixColorB = vec4(255/255.0, 0, 255/255.0, 1);
		mixColorC = vec4(255/255.0, 162/255.0, 223/255.0, 1);
		if(mixFactor<0.6)
		{
			mixFactor=mixFactor/0.6;
			transform =(1 - mixFactor) * mixColorA +  (mixFactor) * mixColorB;
		}
		else
		{
			mixFactor = mixFactor - 0.6;
			mixFactor = mixFactor/0.4;
			transform =(1 - mixFactor) * mixColorB +  (mixFactor) * mixColorC;
		}*/
	  //b=transform.xyz;
	  //caustics to gradient 

			///OVERLAY
			transform.xyz=overlay(a,b);
			transform.w=waveTexel.w;
			//transform.xyz = transform.xyz*(1-waveTexel.g) + waveTexel.g*mixColorC.xyz;
	  	//	 OutputColour.xyz=vec3(255/255.0, 162/255.0, 223/255.0);
		//	 OutputColour.w=waveTexel.w;
		//return;
	 
	 /* if(b.x<midgray) transform.x=2*a.x*b.x+a.x*a.x*(1-2*b.x); else transform.x=2*a.x*(1-b.x)+sqrt(a.x)*(2*b.x-1);
	  if(b.y<midgray) transform.y=2*a.y*b.y+a.y*a.y*(1-2*b.y); else transform.y=2*a.y*(1-b.y)+sqrt(a.y)*(2*b.y-1);
	  if(b.z<midgray) transform.z=2*a.z*b.z+a.z*a.z*(1-2*b.z); else transform.z=2*a.z*(1-b.z)+sqrt(a.z)*(2*b.z-1);
	  */
	  //if(b.y<midgray) transform.y=2*a.y*b.y; else transform.y=(1-2*(1-a.y)*(1-b.y));
	 // if(b.z<midgray) transform.z=2*a.z*b.z; else transform.z=(1-2*(1-a.z)*(1-b.z));
	 //transform.xyz=(vec3(1,1,1)-2*b)*a*a+2*b*a;
	 // transform.xyz = transform.xyz * 0 +(1-2*(1-a)*(1-b));
	  transform.xyz = a + 0.25*transform.xyz;
	  

		transform.w = waveTexel.w;
		transform.xyz = transform.xyz*(1-waveTexel.g) + waveTexel.g*vec3(255/255.0, 162/255.0, 223/255.0);
		transform.xyz = transform.xyz*(1-waveTexel.r)+ waveTexel.r*vec3(1,1,1);

		//transform.w=1;
		if(waveTexelCoordinatesY<0) transform.w=0;
		/*if (topOfWaterHeight + InputWaterLevel>(1 - FragmentTextureMapping.y))
        {
            transform.w = 0;
        }*/
        // Output new colour
        OutputColour = transform;
    }
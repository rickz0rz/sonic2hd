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
	uniform mat4 ProjectionMatrix;
	uniform sampler2D InputTexture;
	const float InputWeight[5] = float[](0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162);
	uniform int InputAxis;

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
	vec4 BlurX()
	{
		ivec2 pix = ivec2(gl_FragCoord.xy);
		vec4 sum = texelFetch(InputTexture, pix, 0) * InputWeight[0];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2( 1, 0)) * InputWeight[1];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(-1, 0)) * InputWeight[1];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2( 2, 0)) * InputWeight[2];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(-2, 0)) * InputWeight[2];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2( 3, 0)) * InputWeight[3];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(-3, 0)) * InputWeight[3];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2( 4, 0)) * InputWeight[4];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(-4, 0)) * InputWeight[4];
		return sum;
	}

	vec4 BlurY()
	{
		ivec2 pix = ivec2(gl_FragCoord.xy);
		vec4 sum = texelFetch(InputTexture, pix, 0) * InputWeight[0];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0,  1)) * InputWeight[1];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0, -1)) * InputWeight[1];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0,  2)) * InputWeight[2];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0, -2)) * InputWeight[2];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0,  3)) * InputWeight[3];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0, -3)) * InputWeight[3];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0,  4)) * InputWeight[4];
		sum += texelFetchOffset(InputTexture, pix, 0, ivec2(0, -4)) * InputWeight[4];
		return sum;
	}

	void main()
	{
		if (InputAxis == 0)
			OutputColour = BlurX();
		else
			OutputColour = BlurY();
	}
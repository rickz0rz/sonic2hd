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
	uniform float InputDelta;

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
	void main()
	{
		vec4 texel = texture(InputTexture, FragmentTextureMapping);
		texel.rgb += vec3(InputDelta);
		OutputColour = texel;
	}
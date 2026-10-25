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
	uniform mat4 ModelViewMatrix;
	uniform sampler2D InputTexture;
	uniform vec3 InputLightSource;
	uniform float MinimumLight;
	uniform vec4 Tint;

@vertex_input:
	layout (location = 0) in vec3 VertexPosition;
	layout (location = 1) in vec2 VertexTextureMapping;

@vertex_output:
	out vec4 FragmentColour;
	out vec2 FragmentTextureMapping;

@fragment_input:
	in vec4 FragmentColour;
	in vec2 FragmentTextureMapping;
	
@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
		float distance = 1.0 / length(InputLightSource - VertexPosition) * 10.0;
		float light = clamp(distance, MinimumLight, 1.0);
		// float light = distance;

		FragmentColour = vec4(light, light, light, 1.0);
		FragmentTextureMapping = VertexTextureMapping;
		gl_Position = ProjectionMatrix * ModelViewMatrix * vec4(VertexPosition, 1.0);
	}

@fragment:
	void main()
	{
		OutputColour = FragmentColour * Tint * texture(InputTexture, FragmentTextureMapping);
	}
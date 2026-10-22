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
	uniform sampler2D InputDistortionTexture;
	uniform float InputDistortionAmount; // set by HeatRenderer

@vertex_input:
	layout (location = 0) in vec2 VertexPosition;
	layout (location = 1) in vec2 VertexTextureMapping;
	layout (location = 2) in vec2 DistortionTextureMapping;

@vertex_output:
	out vec2 FragmentTextureMapping;
	out vec2 FragmentDistortionTextureMapping;
	
@fragment_input:
	in vec2 FragmentTextureMapping;
	in vec2 FragmentDistortionTextureMapping;
	
@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
		FragmentTextureMapping = VertexTextureMapping;
		FragmentDistortionTextureMapping = DistortionTextureMapping;
		gl_Position = ProjectionMatrix * vec4(VertexPosition, 0.0, 1.0);
	}

@fragment:
	void main()
	{
		// Get distortion amount
		vec4 distortionTexel = texture(InputDistortionTexture, FragmentDistortionTextureMapping);
		distortionTexel.x = ((distortionTexel.r * 2.0) - 1.0);
		distortionTexel.y = ((distortionTexel.g * 2.0) - 1.0);
		distortionTexel.xy *= distortionTexel.a * InputDistortionAmount;

		// Apply distortion
		vec4 colour = texture(InputTexture, FragmentTextureMapping + distortionTexel.xy);
		colour.a *= distortionTexel.a;
		OutputColour = colour;
		// OutputColour = vec4(1, 1, 1, InputDistortionAmount / 0.01);
	}
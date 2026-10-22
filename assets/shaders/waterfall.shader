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
	uniform float InputDistortionAmount = 0.01;
	uniform vec2 InputNonDistortionRadius;

@vertex_input:
	layout (location = 0) in vec2 VertexPosition;
	layout (location = 1) in vec2 VertexTextureMapping;
	layout (location = 2) in vec2 DistortionTextureMapping;
	layout (location = 3) in vec2 VertexNormalisation;

@vertex_output:
	out vec2 FragmentTextureMapping;
	out vec2 FragmentDistortionTextureMapping;
	out vec2 FragmentNormalisation;
	
@fragment_input:
	in vec2 FragmentTextureMapping;
	in vec2 FragmentDistortionTextureMapping;
	in vec2 FragmentNormalisation;

@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
		FragmentTextureMapping = VertexTextureMapping;
		FragmentDistortionTextureMapping = DistortionTextureMapping;
		FragmentNormalisation = VertexNormalisation;
		gl_Position = ProjectionMatrix * vec4(VertexPosition, 0.0, 1.0);
	}

@fragment:
	void main()
	{
		// Get distortion amount, normalised
		vec4 distortionTexel = texture(InputDistortionTexture, FragmentDistortionTextureMapping);
		distortionTexel.x = 0;
		distortionTexel.y = (distortionTexel.y * 2) - 1;
		distortionTexel.y *= InputDistortionAmount;

		float multiplier = 1.0;
		if (FragmentNormalisation.x < 0.5)
			multiplier *= clamp(FragmentNormalisation.x / InputNonDistortionRadius.x, 0, 1);
		else
			multiplier *= clamp((1 - FragmentNormalisation.x) / InputNonDistortionRadius.x, 0, 1);
		if (FragmentNormalisation.y < 0.5)
			multiplier *= clamp(FragmentNormalisation.y / InputNonDistortionRadius.y, 0, 1);
		else
			multiplier *= clamp((1 - FragmentNormalisation.y) / InputNonDistortionRadius.y, 0, 1);


		distortionTexel.xy *= multiplier;

		// Apply distortion
		vec4 colour = texture(InputTexture, FragmentTextureMapping + distortionTexel.xy);
		colour.a *= distortionTexel.a;
		OutputColour = colour;
	}
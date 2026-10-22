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
	uniform mat4 ModelViewMatrix;
	uniform mat4 ProjectionMatrix;
	uniform int InputTextureCount;
	uniform sampler2D InputTexture[2];
	uniform vec4 InputClipRectangle;
	uniform vec4 InputAdditiveColour;

@vertex_input:
	layout (location = 0) in vec2 VertexPosition;
	layout (location = 1) in vec4 VertexColour;
	layout (location = 2) in vec2 VertexTextureMapping;

@vertex_output:
	out vec4 FragmentColour;
	out vec2 FragmentTextureMapping;
	out vec2 FragmentPosition;

@fragment_input:
	in vec4 FragmentColour;
	in vec2 FragmentTextureMapping;
	in vec2 FragmentPosition;

@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
		FragmentColour = VertexColour;
		FragmentTextureMapping = VertexTextureMapping;

		vec4 modelViewPosition = ModelViewMatrix * vec4(VertexPosition, 0.0, 1.0);
		FragmentPosition = modelViewPosition.xy;
		gl_Position = ProjectionMatrix * modelViewPosition;
	}

@fragment:
	void main()
	{
		// Discard unless in clip rectangle
		if (FragmentPosition.x < InputClipRectangle.x || FragmentPosition.y < InputClipRectangle.y ||
			FragmentPosition.x >= InputClipRectangle.z || FragmentPosition.y >= InputClipRectangle.w
		) {
			discard;
			return;
		}

		// Multiply texture colours together
		vec4 TextureColour = vec4(1.0);
		if (InputTextureCount > 0)
			TextureColour *= texture2D(InputTexture[0], FragmentTextureMapping);
		if (InputTextureCount > 1)
			TextureColour *= texture2D(InputTexture[1], FragmentTextureMapping);

		// Multiply texture colour with blending colour
		OutputColour = FragmentColour * TextureColour + vec4(InputAdditiveColour.rgb, 0);
	}
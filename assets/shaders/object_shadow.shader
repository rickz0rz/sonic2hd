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
	uniform vec4 InputClipRectangle;
	uniform sampler2D InputTexture;
	uniform vec4 InputColourMultiply;

@vertex_input:
	layout (location = 0) in vec2 VertexPosition;
	layout (location = 1) in vec2 VertexTextureMapping;

@vertex_output:
	out vec2 FragmentTextureMapping;
	out vec2 FragmentPosition;
	
@fragment_input:
	in vec2 FragmentTextureMapping;
	in vec2 FragmentPosition;
	
@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
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


		OutputColour = vec4(0.0f,0.0f,0.0f,texture(InputTexture, FragmentTextureMapping).a*0.4f);
	}
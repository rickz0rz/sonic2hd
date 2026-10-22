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
	uniform sampler2D InputTexture[4];
	uniform vec4 InputColour;
	uniform vec4 InputClipRectangle;
	uniform int InputFilter;
	uniform float InputFilterAmount;
	uniform float AlphaGrayscale;
    uniform vec4 ShadowColour;
@vertex_input:
	layout (location = 0) in vec2 VertexPosition;
	layout (location = 1) in vec2 VertexTextureMapping;
	layout (location = 2) in float VertexTextureIndex;
	layout (location = 3) in float VertexOpacity;

@vertex_output:
	out vec2 FragmentTextureMapping;
	flat out int FragmentTextureIndex;
	out vec2 FragmentPosition;
	flat out float FragmentOpacity;
	
@fragment_input:
	in vec2 FragmentTextureMapping;
	flat in int FragmentTextureIndex;
	in vec2 FragmentPosition;
	flat in float FragmentOpacity;
	
@fragment_output:
	layout (location = 0) out vec4 OutputColour;


@vertex:
	void main()
	{
		FragmentTextureMapping = VertexTextureMapping;
		FragmentTextureIndex = int(VertexTextureIndex);
		FragmentOpacity = VertexOpacity;

		vec4 modelViewPosition = ModelViewMatrix * vec4(VertexPosition, 0.0, 1.0);
		FragmentPosition = modelViewPosition.xy;
		gl_Position = ProjectionMatrix * modelViewPosition;
	}

@fragment:
	@include "include/filter.shader"

	void main()
	{
		// Discard unless in clip rectangle
		if (FragmentPosition.x < InputClipRectangle.x || FragmentPosition.y < InputClipRectangle.y ||
			FragmentPosition.x >= InputClipRectangle.z || FragmentPosition.y >= InputClipRectangle.w
		) {
			discard;
			return;
		}

		// Texture
		vec4 texel;
		switch (FragmentTextureIndex) {
		case 0:
			texel = texture(InputTexture[0], FragmentTextureMapping);
			break;
		case 1:
			texel = texture(InputTexture[1], FragmentTextureMapping);
			break;
		case 2:
			texel = texture(InputTexture[2], FragmentTextureMapping);
			break;
		case 3:
			texel = texture(InputTexture[3], FragmentTextureMapping);
			break;
		default:
			texel = vec4(1.0);
		}

		// Multiply texel by input colour
		vec4 result = applyFilter(texel * InputColour, InputFilter, InputFilterAmount);
        result.a *= FragmentOpacity;

		if (AlphaGrayscale == 0.0) {
			OutputColour = result;
		} else {
			OutputColour = result * (1.0 - AlphaGrayscale) + vec4(ShadowColour.r, ShadowColour.g, ShadowColour.b, result.a* ShadowColour.a /** 0.4f*/) * AlphaGrayscale;//result.a * 0.5
			if (OutputColour.a < 0.01)
				discard;
		}
			if (OutputColour.a < 0.001)
				discard;
	}
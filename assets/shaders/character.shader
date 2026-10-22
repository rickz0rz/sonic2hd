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
	uniform sampler2D InputTextureSkin;
	uniform sampler2D InputTextureBody;
	uniform vec3 InputHSLShift;
	uniform int InputFilter;
	uniform float InputFilterAmount;
	uniform float AlphaGrayscale;
	uniform float InputBrightness;
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
	@include "include/filter.shader"
	@include "include/hsl.shader"

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

	vec4 alphaBlend(in vec4 below, in vec4 above)
	{
		vec4 result;
		result.a = above.a + below.a * (1 - above.a);
		result.rgb = ((above.rgb * above.a) + (below.rgb * below.a * (1 - above.a))) / result.a;
		return result;
	}

	void main()
	{
		// Discard unless in clip rectangle
		if (FragmentPosition.x < InputClipRectangle.x || FragmentPosition.y < InputClipRectangle.y ||
			FragmentPosition.x >= InputClipRectangle.z || FragmentPosition.y >= InputClipRectangle.w
		) {
			discard;
			return;
		}

		// Skin colour
		vec4 skinColour = texture(InputTextureSkin, FragmentTextureMapping);
		if (InputHSLShift != vec3(0)) {
			skinColour = rgbToHsl(skinColour);
			skinColour.xyz += InputHSLShift.xyz;
			skinColour = hslToRgb(skinColour);
		}

		// Body colour
		vec4 bodyColour = texture(InputTextureBody, FragmentTextureMapping);

		// Alpha blend body over skin
		OutputColour = applyFilter(alphaBlend(skinColour, bodyColour), InputFilter, InputFilterAmount);
		OutputColour = OutputColour*(1-AlphaGrayscale)+vec4(0,0,0,OutputColour.a*0.4f)*AlphaGrayscale;

		if (InputBrightness < 0) {
			OutputColour = alphaBlend(OutputColour, vec4(0, 0, 0, -InputBrightness * OutputColour.a));
		} else {
			OutputColour = alphaBlend(OutputColour, vec4(1, 0.5, 0,  InputBrightness * OutputColour.a));
		}
		if (OutputColour.a < 0.001)
			discard;
	}
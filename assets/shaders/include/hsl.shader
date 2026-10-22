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

vec4 rgbToHsl(in vec4 inputColour)
{
	vec4 result;

	float rgb_min = min(inputColour.r, min(inputColour.g, inputColour.b));
	float rgb_max = max(inputColour.r, max(inputColour.g, inputColour.b));
	result.z = (rgb_max + rgb_min) / 2.0;

	if (rgb_max == rgb_min) {
		result.x = result.y = 0.0; // achromatic
	} else {
		float d = rgb_max - rgb_min;
		result.y = result.z > 0.5 ?
			d / (2 - rgb_max - rgb_min) :
			d / (rgb_max + rgb_min);
		if (rgb_max == inputColour.r)
			result.x = (inputColour.g - inputColour.b) / d + (inputColour.g < inputColour.b ? 6.0 : 0.0);
		else if (rgb_max == inputColour.g)
			result.x = (inputColour.b - inputColour.r) / d + 2.0;
		else if (rgb_max == inputColour.b)
			result.x = (inputColour.r - inputColour.g) / d + 4.0;
		result.x /= 6.0;
	}

	// Copy alpha
	result.a = inputColour.a;

	return result;
}

float hueToRgb(in float p, in float q, in float t)
{
	if (t < 0.0) t += 1.0;
	if (t > 1.0) t -= 1.0;
	if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
	if (t < 1.0 / 2.0) return q;
	if (t < 2.0 / 3.0) return p + (q - p) * ((2.0 / 3.0) - t) * 6.0;
	return p;
}

vec4 hslToRgb(in vec4 inputColour)
{
	vec4 result;

	if (inputColour.y == 0.0) {
		result.r = result.g = result.b = 1.0; // achromatic
	} else {
		float q = inputColour.z < 0.5 ?
			inputColour.z * (1 + inputColour.y) :
			inputColour.z + inputColour.y - (inputColour.z * inputColour.y);
		float p = 2.0 * inputColour.z - q;
		result.r = hueToRgb(p, q, inputColour.x + (1.0 / 3.0));
		result.g = hueToRgb(p, q, inputColour.x);
		result.b = hueToRgb(p, q, inputColour.x - (1.0 / 3.0));
	}

	// Copy alpha
	result.a = inputColour.a;

	return result;
}
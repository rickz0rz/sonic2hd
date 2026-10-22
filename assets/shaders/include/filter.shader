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

vec4 applyNightFilter(in vec4 colour, in float amount)
{
	float redGreenAmount = 0.5;
	float desatuateAmount = 0.5;
	float lightness = 0.8;
	
	// Get luma value
	float luma = (colour.r * 0.21) + (colour.g * 0.71) + (colour.b * 0.07);
 
	// Darken slightly
	luma = luma * lightness;
 
	// Desatuate
	vec4 result = vec4(
		mix(colour.r, luma, desatuateAmount),
		mix(colour.g, luma, desatuateAmount),
		mix(colour.b, luma, desatuateAmount),
		colour.a
	);

	vec3 blueTint = result.rgb;
	blueTint.r *= redGreenAmount * 0.21;
	blueTint.g *= redGreenAmount * 0.71;

	float mixAmount = amount;
	result.r = mix(result.r, blueTint.r, mixAmount);
	result.g = mix(result.g, blueTint.g, mixAmount);
	result.b = mix(result.b, blueTint.b, mixAmount);

	// Transision between original colour and night colour
	result.r = mix(colour.r, result.r, amount);
	result.g = mix(colour.g, result.g, amount);
	result.b = mix(colour.b, result.b, amount);
	return result;
}

vec4 applyDarkFilter(in vec4 colour, in float amount)
{
	float desatuateAmount = 0.5;
	float lightness = 0;
	
	// Get luma value
	float luma = (colour.r * 0.21) + (colour.g * 0.71) + (colour.b * 0.07);
 
	// Darken slightly
	luma = luma * lightness;
 
	// Desatuate
	vec4 result = vec4(
		mix(colour.r, luma, desatuateAmount),
		mix(colour.g, luma, desatuateAmount),
		mix(colour.b, luma, desatuateAmount),
		colour.a
	);

	// Transision between original colour and night colour
	result.r = mix(colour.r, result.r, amount);
	result.g = mix(colour.g, result.g, amount);
	result.b = mix(colour.b, result.b, amount);
	return result;
}

vec4 applyFilter(in vec4 colour, in int filterType, in float amount)
{
	switch (filterType) {
	case 1:  return applyNightFilter(colour, amount);
	case 2:  return applyDarkFilter(colour, amount);
	default: return colour;
	}
}
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
	const int LineSpacing = 12;

	uniform mat4 ProjectionMatrix;

@vertex_input:
	layout (location = 0) in vec2 VertexPosition;

@vertex_output:
	out vec2 FragmentPosition;
	
@fragment_input:
	in vec2 FragmentPosition;
	
@fragment_output:
	layout (location = 0) out vec4 OutputColour;

@vertex:
	void main()
	{
		FragmentPosition = VertexPosition;
		gl_Position = ProjectionMatrix * vec4(VertexPosition, 0.0, 1.0);
	}

@fragment:
	void main()
	{
		if (!(int(FragmentPosition.x) % LineSpacing == (int(FragmentPosition.y) % LineSpacing)))
			discard;

		// Output new colour
		OutputColour = vec4(vec3(0.5), 1.0);
	}
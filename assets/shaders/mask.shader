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
    uniform mat4 IntersectionModelViewMatrix;
    uniform mat4 TargetModelViewMatrix;
    uniform mat4 MaskModelViewMatrix;
    uniform mat4 ProjectionMatrix;
    uniform sampler2D InputTexture;
    uniform sampler2D InputTextureMask;
    uniform vec4 InputColour;
    uniform int MaskInput;
	uniform int MaskColorMultiply;

@vertex_input:
    layout (location = 0) in vec2 VertexPosition;
    layout (location = 1) in vec2 VertexTextureMapping;
    layout (location = 2) in vec2 VertexMaskTextureMapping;

@vertex_output:
    out vec2 FragmentPosition;
    out vec2 FragmentTextureMapping;
    out vec2 FragmentMaskTextureMapping;
    
@fragment_input:
    in vec2 FragmentTextureMapping;
    in vec2 FragmentMaskTextureMapping;
    
@fragment_output:
    layout (location = 0) out vec4 OutputColour;

@vertex:
    void main()
    {
        if (MaskInput > 0)
        {
            vec4 rotatedTarget =  TargetModelViewMatrix * vec4(VertexTextureMapping, 0.0, 1.0);
            vec4 rotatedMask = MaskModelViewMatrix * vec4(VertexMaskTextureMapping, 0.0, 1.0);

            FragmentTextureMapping = vec2(rotatedTarget.x, rotatedTarget.y);
            FragmentMaskTextureMapping = vec2(rotatedMask.x, rotatedMask.y);

            vec4 modelViewPosition = IntersectionModelViewMatrix * vec4(VertexPosition, 0.0, 1.0);
            gl_Position = ProjectionMatrix * modelViewPosition;                
        }
        else
        {
            FragmentTextureMapping = VertexTextureMapping;

            vec4 modelViewPosition = TargetModelViewMatrix * vec4(VertexPosition, 0.0, 1.0);
            gl_Position = ProjectionMatrix * modelViewPosition;
        }
    }

@fragment:
    void main()
    {
        vec4 texel = vec4(1.0);
		vec4 texelColor = vec4(1.0);

        if (MaskInput > 0)
        {
            texel = texture(InputTexture, FragmentTextureMapping);

            vec4 texelMask = texture(InputTextureMask, FragmentMaskTextureMapping);

			texelColor = vec4(InputColour * texel);
            
			if (MaskColorMultiply > 0) {
				texelColor = vec4(texelColor * texelMask);
			}
			else {
				texelColor.w = texelColor.w * texelMask.w;
			}
        }
        else{
            texel = texture(InputTexture, FragmentTextureMapping);

			texelColor = vec4(InputColour * texel);
            
            OutputColour = texelColor; 
        }

		OutputColour = texelColor;
    }
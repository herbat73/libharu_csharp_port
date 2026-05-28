# Haru Free PDF Library C# full managed code port 

  **URL http://libharu.org/**

  **Copyright 2000-2006 (c) Takeshi Kanno**

  **Copyright 2007-2009 (c) Antony Dovgal et al.**

  **Copyright 2026 (c) Port to C# Adam Adamczyk + Codex with ChatGPT 5.5 model**

See [PORTING](PORTING.md) for porting info and instructions on how to install libHaru C#.


# What is Haru Free PDF Library?

Haru is a free, cross platform, open-sourced software library for generating 
PDF. It supports the following features.

   1. Generating PDF files with lines, text, images.
   2. Outline, text annotation, link annotation.
   3. Compressing document with deflate-decode.
   4. Embedding PNG, Jpeg images.
   5. Embedding Type1 font and TrueType font.
   6. Creating encrypted PDF files.
   7. Using various character set (ISO8859-1\~16, MSCP1250\~8, KOI8-R).
   8. Supporting CJK fonts and encodings.

You can add the feature of PDF creation by using Haru without understanding 
complicated internal structure of PDF.


# The differences from the orginal version 


The biggest differences are that all code is written in C# as result of full port of orginal [LibHaru C](https://github.com/libharu/libharu) library.


# License

Haru is distributed under the ZLIB/LIBPNG License. Because ZLIB/LIBPNG License 
is one of the freest licenses, You can use Haru for various purposes.

The license of Haru is as follows.

Copyright (C) 1999-2006 Takeshi Kanno
Copyright (C) 2007-2009 Antony Dovgal
Copyright 2026 (c) Adam Adamczyk

This software is provided 'as-is', without any express or implied warranty.

In no event will the authors be held liable for any damages arising from the 
use of this software.

Permission is granted to anyone to use this software for any purpose,including 
commercial applications, and to alter it and redistribute it freely, subject 
to the following restrictions:

 1. The origin of this software must not be misrepresented; you must not claim 
    that you wrote the original software. If you use this software in a 
    product, an acknowledgment in the product documentation would be 
    appreciated but is not required.
 2. Altered source versions must be plainly marked as such, and must not be 
    misrepresented as being the original software.
 3. This notice may not be removed or altered from any source distribution.



# Acknowledgment

1. Information-technology Promotion Agency(IPA)
   The development of Haru has been supported by Exploratory Software Project 
   of Information-technology Promotion Agency(IPA), Japan. 

2. All users of libHaru.
   We wish to thank all users of Haru.
   In particular, we thank Thomas Nimstad, LeslieM, Par Hogberg, adenelson, 
   Riccardo Cohen, sea_sbs, Andrew. 
   They gave me very useful advice.

3. Adobe Systems Inc.
   We thank Adobe Systems Inc. for publishing PDF specification.

4. ChatGpt.com for providing Codex along with the ChatGPT 5.5 model for migration from c to c# process   

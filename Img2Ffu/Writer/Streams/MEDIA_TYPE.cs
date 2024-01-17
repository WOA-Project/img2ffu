/*

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000
Copyright (c) 2018, Proto Beta Test - protobetatest.com - @ProtoBetaTest

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
namespace Img2Ffu.Streams
{
    public partial class DeviceStream
    {
        private enum MEDIA_TYPE : int
        {
            Unknown = 0,
            F5_1Pt2_512 = 1,
            F3_1Pt44_512 = 2,
            F3_2Pt88_512 = 3,
            F3_20Pt8_512 = 4,
            F3_720_512 = 5,
            F5_360_512 = 6,
            F5_320_512 = 7,
            F5_320_1024 = 8,
            F5_180_512 = 9,
            F5_160_512 = 10,
            RemovableMedia = 11,
            FixedMedia = 12,
            F3_120M_512 = 13,
            F3_640_512 = 14,
            F5_640_512 = 15,
            F5_720_512 = 16,
            F3_1Pt2_512 = 17,
            F3_1Pt23_1024 = 18,
            F5_1Pt23_1024 = 19,
            F3_128Mb_512 = 20,
            F3_230Mb_512 = 21,
            F8_256_128 = 22,
            F3_200Mb_512 = 23,
            F3_240M_512 = 24,
            F3_32M_512 = 25
        }
    }
}
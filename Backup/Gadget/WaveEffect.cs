
#define _BUFFERED_RENDERING
#define _JAGGED_ARRAYS
//#define _RECTANGULAR_ARRAYS
//#define _LINEAR_ARRAYS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Reflection.Emit;
using System.Reflection;

namespace URWPGSim2D.Gadget
{
    public class WaveEffect
    {
        public static WaveEffect Instance()
        {
            if (instance == null)
            {
                instance = new WaveEffect();
            }
            return instance;
        }
        private WaveEffect() { }
        List<DropData> DropList = new List<DropData>();

        public static FastBitmap _image = null;
        private static FastBitmap _originalImage = null;
        public static int _currentHeightBuffer = 0;
        public static int _newHeightBuffer = 0;
        private static byte[] _bitmapOriginalBytes;
        private static Random _r = new Random();

        // 初始化
        public void LoadBmp(Bitmap bmp)
        {
            _BITMAP_WIDTH = bmp.Width;
            _BITMAP_HEIGHT = bmp.Height;

#if _JAGGED_ARRAYS
            _waveHeight = new int[_BITMAP_WIDTH][][];
            for (int i = 0; i < _BITMAP_WIDTH; i++)
            {
                _waveHeight[i] = new int[_BITMAP_HEIGHT][];
                for (int j = 0; j < _BITMAP_HEIGHT; j++)
                {
                    _waveHeight[i][j] = new int[2];
                }
            }
#endif
#if _RECTANGULAR_ARRAYS
            _waveHeight = new int[_BITMAP_WIDTH, _BITMAP_HEIGHT, 2];
#endif

#if _LINEAR_ARRAYS
            _waveHeight = new int[_BITMAP_WIDTH * _BITMAP_HEIGHT * 2];
#endif

            CreateBitmap(bmp);
        }

        // 输出
        public Bitmap GetBitmap()
        {
            PaintWater();
            return (Bitmap)_image.Bitmap.Clone();
        }
        // 绘图
        public void DropFish(int x, int y, int len, float dir)
        {
            int unitlen = 10;
            for (int i = -len / 2; i < len / 2; i += unitlen)
                DropWater(x - (int)(Math.Cos(dir) * i), y - (int)(Math.Sin(dir) * i), 10, _r.Next(20) + 10);
        }

        public void DropWater(int x, int y, int radius, int height)
        {
            const int ListLength = 50;
            if (DropList.Count > ListLength)
            {
                DropList.RemoveAt(0);
            }
            foreach (DropData d in DropList)
            {
                if (Math.Abs(x - d.x) + Math.Abs(y - d.y) < 8)
                    return;
            }
            long _distance;
            int _x;
            int _y;
            Single _ratio;

            _ratio = (Single)((Math.PI / (Single)radius));

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    _x = x + i;
                    _y = y + j;
                    if ((_x >= 0) && (_x <= _BITMAP_WIDTH - 1) && (_y >= 0) && (_y <= _BITMAP_HEIGHT - 1))
                    {
                        _distance = (long)Math.Sqrt(i * i + j * j);
                        if (_distance <= radius)
                        {
#if _JAGGED_ARRAYS
                            _waveHeight[_x][_y][_currentHeightBuffer] = (int)(height * Math.Cos((Single)_distance * _ratio));
#endif
#if _RECTANGULAR_ARRAYS
                            _waveHeight[_x,_y,_currentHeightBuffer] = (int)(height * Math.Cos((Single)_distance * _ratio));
#endif
#if _LINEAR_ARRAYS
                            _waveHeight[INDEX3D(_x, _y, _currentHeightBuffer)] = (int)(height * Math.Cos((Single)_distance * _ratio));
#endif
                        }
                    }
                }
            }
            DropList.Add(new DropData(x, y));
        }

        // 刷新
        public void PaintWater()
        {
            _newHeightBuffer = (_currentHeightBuffer + 1) % 2;
            _image.LockBits();
#if _BUFFERED_RENDERING
            byte[] _bufferBits = new byte[_BITS * _image.Width() * _image.Height()];
            Marshal.Copy(_image.Data().Scan0, _bufferBits, 0, _bufferBits.Length);
#endif
            int _offX;
            int _offY;

            for (int _x = 1; _x < _BITMAP_WIDTH - 1; _x++)
            {
                for (int _y = 1; _y < _BITMAP_HEIGHT - 1; _y++)
                {
#if _JAGGED_ARRAYS
                    //
                    //  Simulate movement.
                    //
                    unchecked
                    {
                        _waveHeight[_x][_y][_newHeightBuffer] = ((
                            _waveHeight[_x - 1][_y][_currentHeightBuffer] +
                            _waveHeight[_x - 1][_y - 1][_currentHeightBuffer] +
                            _waveHeight[_x][_y - 1][_currentHeightBuffer] +
                            _waveHeight[_x + 1][_y - 1][_currentHeightBuffer] +
                            _waveHeight[_x + 1][_y][_currentHeightBuffer] +
                            _waveHeight[_x + 1][_y + 1][_currentHeightBuffer] +
                            _waveHeight[_x][_y + 1][_currentHeightBuffer] +
                            _waveHeight[_x - 1][_y + 1][_currentHeightBuffer]) >> 2)
                        - _waveHeight[_x][_y][_newHeightBuffer];
                    }
                    //
                    //  Dampenning.
                    //
                    _waveHeight[_x][_y][_newHeightBuffer] -= (_waveHeight[_x][_y][_newHeightBuffer] >> 5);
                    //
                    //
                    //
                    _offX = ((_waveHeight[_x - 1][_y][_newHeightBuffer] - _waveHeight[_x + 1][_y][_newHeightBuffer])) >> 3;
                    _offY = ((_waveHeight[_x][_y - 1][_newHeightBuffer] - _waveHeight[_x][_y + 1][_newHeightBuffer])) >> 3;
#endif
#if _RECTANGULAR_ARRAYS
                    unchecked
                    {
                        _waveHeight[_x,_y,_newHeightBuffer] = ((
                            _waveHeight[_x - 1,_y,_currentHeightBuffer] +
                            _waveHeight[_x - 1,_y - 1,_currentHeightBuffer] +
                            _waveHeight[_x,_y - 1,_currentHeightBuffer] +
                            _waveHeight[_x + 1,_y - 1,_currentHeightBuffer] +
                            _waveHeight[_x + 1,_y,_currentHeightBuffer] +
                            _waveHeight[_x + 1,_y + 1,_currentHeightBuffer] +
                            _waveHeight[_x,_y + 1,_currentHeightBuffer] +
                            _waveHeight[_x - 1,_y + 1,_currentHeightBuffer]) >> 2)
                        - _waveHeight[_x,_y,_newHeightBuffer];
                    }
                    //
                    //  Dampenning.
                    //
                    _waveHeight[_x,_y,_newHeightBuffer] -= (_waveHeight[_x,_y,_newHeightBuffer] >> 5);
                    //
                    //
                    //
                    _offX = ((_waveHeight[_x - 1,_y,_newHeightBuffer] - _waveHeight[_x + 1,_y,_newHeightBuffer])) >> 4;
                    _offY = ((_waveHeight[_x,_y - 1,_newHeightBuffer] - _waveHeight[_x,_y + 1,_newHeightBuffer])) >> 4;
#endif
#if _LINEAR_ARRAYS
                    unchecked
                    {
                        _waveHeight[INDEX3D(_x,_y, _newHeightBuffer)] = ((
                            _waveHeight[INDEX3D(_x - 1, _y + 0, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x - 1, _y - 1, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x - 0, _y - 1, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x + 1, _y - 1, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x + 1, _y + 0, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x + 1, _y + 1, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x + 0, _y + 1, _currentHeightBuffer)] +
                            _waveHeight[INDEX3D(_x - 1, _y + 1, _currentHeightBuffer)]) >> 2)
                        - _waveHeight[INDEX3D(_x, _y, _newHeightBuffer)];
                    }
                    //
                    //  Dampenning.
                    //
                    _waveHeight[INDEX3D(_x, _y, _newHeightBuffer)] -= (_waveHeight[INDEX3D(_x, _y, _newHeightBuffer)] >> 5);
                    //
                    //
                    //
                    _offX = ((_waveHeight[INDEX3D(_x - 1, _y - 0, _newHeightBuffer)] - _waveHeight[INDEX3D(_x + 1, _y + 0, _newHeightBuffer)])) >> 4;
                    _offY = ((_waveHeight[INDEX3D(_x + 0, _y - 1, _newHeightBuffer)] - _waveHeight[INDEX3D(_x + 0, _y + 1, _newHeightBuffer)])) >> 4;
#endif
                    //
                    //  Nothing to do
                    //
                    if ((_offX == 0) && (_offY == 0)) continue;
                    //
                    //  Fix boundaries
                    //
                    if (_x + _offX <= 0) _offX = -_x;
                    if (_x + _offX >= _BITMAP_WIDTH - 1) _offX = _BITMAP_WIDTH - _x - 1;
                    if (_y + _offY <= 0) _offY = -_y;
                    if (_y + _offY >= _BITMAP_HEIGHT - 1) _offY = _BITMAP_HEIGHT - _y - 1;
                    //
                    //  
                    //
#if _BUFFERED_RENDERING
                    _bufferBits[_BITS * (_x + _y * _BITMAP_WIDTH) + 0] = _bitmapOriginalBytes[_BITS * (_x + _offX + (_y + _offY) * _BITMAP_WIDTH) + 0];
                    _bufferBits[_BITS * (_x + _y * _BITMAP_WIDTH) + 1] = _bitmapOriginalBytes[_BITS * (_x + _offX + (_y + _offY) * _BITMAP_WIDTH) + 1];
                    _bufferBits[_BITS * (_x + _y * _BITMAP_WIDTH) + 2] = _bitmapOriginalBytes[_BITS * (_x + _offX + (_y + _offY) * _BITMAP_WIDTH) + 2];
                    // I dont not implement the ALPHA as previous version did. you can if you want.
                    //_bufferBits[_BITS * (_x + _y * _BITMAP_WIDTH) + 3] = alpha                    
#else
                    _image.SetPixel(_x, _y, _originalImage.GetPixel(_x + _offX, _y + _offY));
#endif
                }
            }
#if _BUFFERED_RENDERING
            Marshal.Copy(_bufferBits, 0, _image.Data().Scan0, _bufferBits.Length);
#endif
            _currentHeightBuffer = _newHeightBuffer;
        }
        // Random
        public int GetRandom( int n ){
            return _r.Next( n );
        }

        private void CreateBitmap(Bitmap bmp)
        {
            _originalImage = new FastBitmap((Bitmap)bmp.Clone(), _BITS);
            _originalImage.LockBits();
            _image = new FastBitmap((Bitmap)bmp.Clone(), _BITS);
            _bitmapOriginalBytes = new byte[_BITS * _image.Width() * _image.Height()];
            _image.LockBits();
            Marshal.Copy(_image.Data().Scan0, _bitmapOriginalBytes, 0, _bitmapOriginalBytes.Length);
            _image.Release();
        }


#if _LINEAR_ARRAYS
        private int INDEX3D(int x, int y, int z) { unchecked { return x * _BITMAP_HEIGHT * 2 + y * 2 + z; } }
#endif

        private struct DropData
        {
            public int x;
            public int y;
            public DropData(int x_, int y_)
            {
                x = x_;
                y = y_;
            }
        }

        private static int _BITMAP_WIDTH = 0;
        private static int _BITMAP_HEIGHT = 0;
        private static int _BITS = 4; /* Dont change this, it 24 bit bitmaps are not supported*/
        private static WaveEffect instance = null;

#if _JAGGED_ARRAYS
        private static int[][][] _waveHeight;
#endif
#if _RECTANGULAR_ARRAYS
        private static int[,,] _waveHeight;
#endif
#if _LINEAR_ARRAYS
        private static int[] _waveHeight;
#endif

    }
}

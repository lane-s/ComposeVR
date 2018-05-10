using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{
    public class DequeTest : MonoBehaviour
    {

        private const int N = 5;
        Deque<int> testDeque;

        // Use this for initialization
        void Awake()
        {
            testDeque = new Deque<int>(N);

            //Test push, pop, and get when initial size is not exceeded
            testDeque.PushFront(1);
            testDeque.PushBack(-1);
            testDeque.PushFront(2);
            testDeque.PushFront(-2);
            testDeque.PushFront(3);

            Debug.Assert(testDeque.Get(0) == 3);
            Debug.Assert(testDeque.Get(1) == -2);
            Debug.Assert(testDeque.Get(2) == 2);
            Debug.Assert(testDeque.Get(3) == 1);
            Debug.Assert(testDeque.Get(4) == -1);

            Debug.Assert(testDeque.PopFront() == 3);
            Debug.Assert(testDeque.PopBack() == -1);
            Debug.Assert(testDeque.PopBack() == 1);
            Debug.Assert(testDeque.PopBack() == 2);
            Debug.Assert(testDeque.PopFront() == -2);
        }
    }
}

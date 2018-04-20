using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Implements a deque using a circular buffer
/// </summary>
/// <typeparam name="T"></typeparam>
public class Deque<T> {

    public int Count {
        set { numElements = value; }
        get { return numElements; }
    }
    private int numElements;

    private T[] buffer;

    private int front;
    private int back;

    public Deque(int size) {
        buffer = new T[Math.Max(size, 1)];
        front = -1;
        back = 0;
        numElements = 0;
    }

    public void PushFront(T item) {
        if(numElements == buffer.Length) {
            ResizeBuffer();
        }
        front = (front + 1) % buffer.Length;
        buffer[front] = item;

        numElements += 1;
    }

    public void PushBack(T item) {
        if(numElements == buffer.Length){
            ResizeBuffer();
        }

        back -= 1;
        if(back < 0) {
            back = buffer.Length - 1;
        }
        buffer[back] = item;

        numElements += 1;
    }

    private void ResizeBuffer() {
        T[] oldBuff = buffer;
        buffer = new T[oldBuff.Length * 2];

        for(int i = 0; i <= front; i++) {
            buffer[front] = oldBuff[i];
        }

        if(front < back) {
            int toEnd = oldBuff.Length - back;
            for(int i = 0; i < toEnd; i++) {
                buffer[buffer.Length - 1 - i] = oldBuff[oldBuff.Length - i - 1];
            }
        }
    }

    public T PopFront() {
        if(numElements == 0) {
            return default(T);
        }

        T frontElem = PeekFront();

        buffer[front] = default(T);

        front -= 1;
        if(front < 0) {
            front = buffer.Length - 1;
        }

        numElements -= 1;
        return frontElem;
    }

    public T PopBack() {
        if(numElements == 0) {
            return default(T);
        }

        T backElem = PeekBack();

        buffer[back] = default(T);
        back = (back + 1) % buffer.Length;

        numElements -= 1;
        return backElem;
    }

    public T PeekFront() {
        if(numElements == 0) {
            return default(T);
        }
        return buffer[front];
    }

    public T PeekBack() {
        if(numElements == 0) {
            return default(T);
        }
        return buffer[back];
    }

    public T Get(int i) {
        if(i < 0 || i >= numElements) {
            return default(T);
        }

        int idx = front - i;
        if(idx < 0) {
            idx = buffer.Length + idx;
        }

        return buffer[idx];
    }
}

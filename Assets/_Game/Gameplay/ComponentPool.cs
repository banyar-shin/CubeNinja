using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeNinja.Gameplay
{
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Stack<T> available = new Stack<T>();
        private readonly Func<T> factory;

        public ComponentPool(Func<T> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public int Count => available.Count;

        public T Get()
        {
            var item = available.Count > 0 ? available.Pop() : factory();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
            {
                return;
            }

            item.gameObject.SetActive(false);
            available.Push(item);
        }
    }
}

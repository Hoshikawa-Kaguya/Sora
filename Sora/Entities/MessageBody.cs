using System;
using System.Collections;
using System.Collections.Generic;
using Sora.Entities.MessageElement;
using Sora.Enumeration;

namespace Sora.Entities
{
    /// <summary>
    /// 消息段
    /// </summary>
    public class MessageBody : IList<CQCode>
    {
        #region 私有字段

        private readonly List<CQCode> _message = new();

        #endregion

        #region 属性

        /// <summary>
        /// 消息段数量
        /// </summary>
        public int Count => _message.Count;

        /// <summary>
        /// 只读
        /// </summary>
        public bool IsReadOnly => false;

        #endregion

        #region 构造方法

        /// <summary>
        /// 构造消息段列表
        /// </summary>
        public MessageBody(IEnumerable<CQCode> messages)
        {
            _message.Clear();
            _message.AddRange(messages);
        }

        /// <summary>
        /// 构造消息段列表
        /// </summary>
        public MessageBody()
        {
        }

        #endregion

        #region List方法

        /// <summary>
        /// 迭代器
        /// </summary>
        IEnumerator<CQCode> IEnumerable<CQCode>.GetEnumerator()
        {
            return _message.GetEnumerator();
        }

        /// <summary>
        /// 迭代器
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return _message.GetEnumerator();
        }

        /// <summary>
        /// 添加消息段
        /// </summary>
        /// <param name="item"></param>
        public void Add(CQCode item)
        {
            _message.Add(item);
        }

        /// <summary>
        /// 清空消息段
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Clear()
        {
            _message.Clear();
        }

        /// <summary>
        /// 判断包含
        /// </summary>
        /// <param name="item">消息段</param>
        public bool Contains(CQCode item)
        {
            return _message.Contains(item);
        }

        /// <summary>
        /// 复制
        /// </summary>
        /// <param name="array">消息段数组</param>
        /// <param name="arrayIndex">索引</param>
        public void CopyTo(CQCode[] array, int arrayIndex)
        {
            _message.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 删除消息段
        /// </summary>
        /// <param name="item">消息段</param>
        public bool Remove(CQCode item)
        {
            return _message.Remove(item);
        }

        /// <summary>
        /// 索引查找
        /// </summary>
        /// <param name="item">消息段</param>
        public int IndexOf(CQCode item)
        {
            return _message.IndexOf(item);
        }

        /// <summary>
        /// 插入消息段
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="item">消息段</param>
        public void Insert(int index, CQCode item)
        {
            _message.Insert(index, item);
        }

        /// <summary>
        /// 删除消息段
        /// </summary>
        /// <param name="index">索引</param>
        public void RemoveAt(int index)
        {
            _message.RemoveAt(index);
        }

        /// <summary>
        /// 转普通列表
        /// </summary>
        public List<CQCode> ToList()
        {
            return _message;
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// <para>添加纯文本消息段</para>
        /// <para>CQ码消息段扩展</para>
        /// </summary>
        /// <param name="text">纯文本信息</param>
        public void AddText(string text) =>
            _message.Add(CQCodes.CQText(text));

        #endregion

        #region 运算重载

        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="index">索引</param>
        /// <exception cref="ArgumentOutOfRangeException">索引超出范围</exception>
        /// <exception cref="NullReferenceException">读取到了空消息段</exception>
        public CQCode this[int index]
        {
            get
            {
                if (_message.Count == 0 || index > _message.Count - 1 || index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _message[index];
            }
            set
            {
                if (value.MessageType == CQType.Unknown)
                    throw new NullReferenceException("message element is null");
                _message[index] = value;
            }
        }

        #endregion
    }
}
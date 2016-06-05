﻿using AutoComplete.Core.DataStructure;
using AutoComplete.Core.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AutoComplete.Core
{
    internal class TrieSerializer
    {
        public static void SerializeHeaderWithXmlSerializer(Stream header, TrieIndexHeader trieIndexHeader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TrieIndexHeader));
            serializer.Serialize(header, trieIndexHeader);
        }

        public static TrieIndexHeader DeserializeHeaderWithXmlSerializer(Stream header)
        {
            return DeserializeHeaderWithXmlSerializer(header, false);
        }

        public static TrieIndexHeader DeserializeHeaderWithXmlSerializer(Stream header, bool dontAutoInitializeCache)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TrieIndexHeader));
            var trieIndexHeader = (TrieIndexHeader)serializer.Deserialize(header);

            if (!dontAutoInitializeCache)
                trieIndexHeader.InitCharacterCache();

            return trieIndexHeader;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="trieIndexHeader"></param>
        /// <param name="index"></param>
        /// <remarks>Don't forget to dispose stream</remarks>
        /// <returns></returns>
        public static int SerializeIndexWithBinaryWriter(TrieNode node, TrieIndexHeader trieIndexHeader, Stream index)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            int processedNodeCount = 0;

            BinaryWriter binaryWriter = new BinaryWriter(index);
            Queue<TrieNode> serializerQueue = new Queue<TrieNode>();

            serializerQueue.Enqueue(node);

            while (serializerQueue.Count > 0)
            {
                node = serializerQueue.Dequeue();
                
                if (node == null)
                    throw new SerializationException(string.Format("Value cannot be null ", processedNodeCount));

                long currentPositionOfStream = binaryWriter.BaseStream.Position;

                // write character
                //bw.Write(Encoding.Unicode.GetBytes(node.Character.ToString()));
                UInt16? characterIndex = trieIndexHeader.GetCharacterIndex(node.Character);
                if (characterIndex != null && characterIndex.HasValue)
                    binaryWriter.Write(characterIndex.Value);
                else
                    binaryWriter.Write(Convert.ToUInt16(0)); // Its root

                binaryWriter.Write(node.IsTerminal);

                // write children flags
                // convert 512 bool value to 64 byte value for efficient storage
                BitArray baChildren = new BitArray(trieIndexHeader.COUNT_OF_CHARSET);

                if (node.Children != null)
                {
                    foreach (var item in node.Children)
                    {
                        UInt16? itemIndex = trieIndexHeader.GetCharacterIndex(item.Key);

                        baChildren.Set(itemIndex.Value, true);
                    }
                }

                int[] childrenFlags = new int[trieIndexHeader.COUNT_OF_CHILDREN_FLAGS_IN_BYTES];
                BitArrayHelper.CopyToInt32Array(baChildren, childrenFlags, 0);

                for (int i = 0; i < childrenFlags.Length; i++)
                {
                    binaryWriter.Write(childrenFlags[i]);
                }

                // write children offset
                binaryWriter.Write(node.ChildrenCount * trieIndexHeader.LENGTH_OF_STRUCT);

                if (node.Children != null)
                {
                    foreach (var childNode in node.Children)
                    {
                        serializerQueue.Enqueue(childNode.Value);
                    }
                }

                ++processedNodeCount;
            }

            return processedNodeCount;
        }
    }
}
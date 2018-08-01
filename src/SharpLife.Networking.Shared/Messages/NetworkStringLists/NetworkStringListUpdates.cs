// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: NetworkStringListUpdates.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace SharpLife.Networking.Shared.Messages.NetworkStringLists {

  /// <summary>Holder for reflection information generated from NetworkStringListUpdates.proto</summary>
  public static partial class NetworkStringListUpdatesReflection {

    #region Descriptor
    /// <summary>File descriptor for NetworkStringListUpdates.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static NetworkStringListUpdatesReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Ch5OZXR3b3JrU3RyaW5nTGlzdFVwZGF0ZXMucHJvdG8SN1NoYXJwTGlmZS5O",
            "ZXR3b3JraW5nLlNoYXJlZC5NZXNzYWdlcy5OZXR3b3JrU3RyaW5nTGlzdHMi",
            "OAoOTGlzdEJpbmFyeURhdGESEQoJZGF0YV90eXBlGAEgASgNEhMKC2JpbmFy",
            "eV9kYXRhGAIgASgMIn0KDkxpc3RTdHJpbmdEYXRhEg0KBXZhbHVlGAEgASgJ",
            "ElwKC2JpbmFyeV9kYXRhGAIgASgLMkcuU2hhcnBMaWZlLk5ldHdvcmtpbmcu",
            "U2hhcmVkLk1lc3NhZ2VzLk5ldHdvcmtTdHJpbmdMaXN0cy5MaXN0QmluYXJ5",
            "RGF0YSKDAQoUTGlzdFN0cmluZ0RhdGFVcGRhdGUSDQoFaW5kZXgYASABKA0S",
            "XAoLYmluYXJ5X2RhdGEYAiABKAsyRy5TaGFycExpZmUuTmV0d29ya2luZy5T",
            "aGFyZWQuTWVzc2FnZXMuTmV0d29ya1N0cmluZ0xpc3RzLkxpc3RCaW5hcnlE",
            "YXRhIpYBChtOZXR3b3JrU3RyaW5nTGlzdEZ1bGxVcGRhdGUSDwoHbGlzdF9p",
            "ZBgBIAEoDRIMCgRuYW1lGAIgASgJElgKB3N0cmluZ3MYAyADKAsyRy5TaGFy",
            "cExpZmUuTmV0d29ya2luZy5TaGFyZWQuTWVzc2FnZXMuTmV0d29ya1N0cmlu",
            "Z0xpc3RzLkxpc3RTdHJpbmdEYXRhIuQBChdOZXR3b3JrU3RyaW5nTGlzdFVw",
            "ZGF0ZRIPCgdsaXN0X2lkGAEgASgNElgKB3N0cmluZ3MYAiADKAsyRy5TaGFy",
            "cExpZmUuTmV0d29ya2luZy5TaGFyZWQuTWVzc2FnZXMuTmV0d29ya1N0cmlu",
            "Z0xpc3RzLkxpc3RTdHJpbmdEYXRhEl4KB3VwZGF0ZXMYAyADKAsyTS5TaGFy",
            "cExpZmUuTmV0d29ya2luZy5TaGFyZWQuTWVzc2FnZXMuTmV0d29ya1N0cmlu",
            "Z0xpc3RzLkxpc3RTdHJpbmdEYXRhVXBkYXRlYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData), global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData.Parser, new[]{ "DataType", "BinaryData" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData), global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData.Parser, new[]{ "Value", "BinaryData" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate), global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate.Parser, new[]{ "Index", "BinaryData" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListFullUpdate), global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListFullUpdate.Parser, new[]{ "ListId", "Name", "Strings" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdate), global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdate.Parser, new[]{ "ListId", "Strings", "Updates" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class ListBinaryData : pb::IMessage<ListBinaryData> {
    private static readonly pb::MessageParser<ListBinaryData> _parser = new pb::MessageParser<ListBinaryData>(() => new ListBinaryData());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ListBinaryData> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdatesReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListBinaryData() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListBinaryData(ListBinaryData other) : this() {
      dataType_ = other.dataType_;
      binaryData_ = other.binaryData_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListBinaryData Clone() {
      return new ListBinaryData(this);
    }

    /// <summary>Field number for the "data_type" field.</summary>
    public const int DataTypeFieldNumber = 1;
    private uint dataType_;
    /// <summary>
    ///0 if null data (empty byte string)
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint DataType {
      get { return dataType_; }
      set {
        dataType_ = value;
      }
    }

    /// <summary>Field number for the "binary_data" field.</summary>
    public const int BinaryDataFieldNumber = 2;
    private pb::ByteString binaryData_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pb::ByteString BinaryData {
      get { return binaryData_; }
      set {
        binaryData_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ListBinaryData);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ListBinaryData other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (DataType != other.DataType) return false;
      if (BinaryData != other.BinaryData) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (DataType != 0) hash ^= DataType.GetHashCode();
      if (BinaryData.Length != 0) hash ^= BinaryData.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (DataType != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(DataType);
      }
      if (BinaryData.Length != 0) {
        output.WriteRawTag(18);
        output.WriteBytes(BinaryData);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (DataType != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(DataType);
      }
      if (BinaryData.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(BinaryData);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ListBinaryData other) {
      if (other == null) {
        return;
      }
      if (other.DataType != 0) {
        DataType = other.DataType;
      }
      if (other.BinaryData.Length != 0) {
        BinaryData = other.BinaryData;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            DataType = input.ReadUInt32();
            break;
          }
          case 18: {
            BinaryData = input.ReadBytes();
            break;
          }
        }
      }
    }

  }

  /// <summary>
  ///String index is local count + index in update
  /// </summary>
  public sealed partial class ListStringData : pb::IMessage<ListStringData> {
    private static readonly pb::MessageParser<ListStringData> _parser = new pb::MessageParser<ListStringData>(() => new ListStringData());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ListStringData> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdatesReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringData() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringData(ListStringData other) : this() {
      value_ = other.value_;
      BinaryData = other.binaryData_ != null ? other.BinaryData.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringData Clone() {
      return new ListStringData(this);
    }

    /// <summary>Field number for the "value" field.</summary>
    public const int ValueFieldNumber = 1;
    private string value_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Value {
      get { return value_; }
      set {
        value_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "binary_data" field.</summary>
    public const int BinaryDataFieldNumber = 2;
    private global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData binaryData_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData BinaryData {
      get { return binaryData_; }
      set {
        binaryData_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ListStringData);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ListStringData other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Value != other.Value) return false;
      if (!object.Equals(BinaryData, other.BinaryData)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Value.Length != 0) hash ^= Value.GetHashCode();
      if (binaryData_ != null) hash ^= BinaryData.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Value.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Value);
      }
      if (binaryData_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(BinaryData);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Value.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Value);
      }
      if (binaryData_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(BinaryData);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ListStringData other) {
      if (other == null) {
        return;
      }
      if (other.Value.Length != 0) {
        Value = other.Value;
      }
      if (other.binaryData_ != null) {
        if (binaryData_ == null) {
          binaryData_ = new global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData();
        }
        BinaryData.MergeFrom(other.BinaryData);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Value = input.ReadString();
            break;
          }
          case 18: {
            if (binaryData_ == null) {
              binaryData_ = new global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData();
            }
            input.ReadMessage(binaryData_);
            break;
          }
        }
      }
    }

  }

  public sealed partial class ListStringDataUpdate : pb::IMessage<ListStringDataUpdate> {
    private static readonly pb::MessageParser<ListStringDataUpdate> _parser = new pb::MessageParser<ListStringDataUpdate>(() => new ListStringDataUpdate());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ListStringDataUpdate> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdatesReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringDataUpdate() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringDataUpdate(ListStringDataUpdate other) : this() {
      index_ = other.index_;
      BinaryData = other.binaryData_ != null ? other.BinaryData.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ListStringDataUpdate Clone() {
      return new ListStringDataUpdate(this);
    }

    /// <summary>Field number for the "index" field.</summary>
    public const int IndexFieldNumber = 1;
    private uint index_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint Index {
      get { return index_; }
      set {
        index_ = value;
      }
    }

    /// <summary>Field number for the "binary_data" field.</summary>
    public const int BinaryDataFieldNumber = 2;
    private global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData binaryData_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData BinaryData {
      get { return binaryData_; }
      set {
        binaryData_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ListStringDataUpdate);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ListStringDataUpdate other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Index != other.Index) return false;
      if (!object.Equals(BinaryData, other.BinaryData)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Index != 0) hash ^= Index.GetHashCode();
      if (binaryData_ != null) hash ^= BinaryData.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Index != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(Index);
      }
      if (binaryData_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(BinaryData);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Index != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(Index);
      }
      if (binaryData_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(BinaryData);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ListStringDataUpdate other) {
      if (other == null) {
        return;
      }
      if (other.Index != 0) {
        Index = other.Index;
      }
      if (other.binaryData_ != null) {
        if (binaryData_ == null) {
          binaryData_ = new global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData();
        }
        BinaryData.MergeFrom(other.BinaryData);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Index = input.ReadUInt32();
            break;
          }
          case 18: {
            if (binaryData_ == null) {
              binaryData_ = new global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListBinaryData();
            }
            input.ReadMessage(binaryData_);
            break;
          }
        }
      }
    }

  }

  /// <summary>
  ///Updates a specific network string list
  /// </summary>
  public sealed partial class NetworkStringListFullUpdate : pb::IMessage<NetworkStringListFullUpdate> {
    private static readonly pb::MessageParser<NetworkStringListFullUpdate> _parser = new pb::MessageParser<NetworkStringListFullUpdate>(() => new NetworkStringListFullUpdate());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<NetworkStringListFullUpdate> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdatesReflection.Descriptor.MessageTypes[3]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListFullUpdate() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListFullUpdate(NetworkStringListFullUpdate other) : this() {
      listId_ = other.listId_;
      name_ = other.name_;
      strings_ = other.strings_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListFullUpdate Clone() {
      return new NetworkStringListFullUpdate(this);
    }

    /// <summary>Field number for the "list_id" field.</summary>
    public const int ListIdFieldNumber = 1;
    private uint listId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint ListId {
      get { return listId_; }
      set {
        listId_ = value;
      }
    }

    /// <summary>Field number for the "name" field.</summary>
    public const int NameFieldNumber = 2;
    private string name_ = "";
    /// <summary>
    ///Lets the client patch up the correct index for a table
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Name {
      get { return name_; }
      set {
        name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "strings" field.</summary>
    public const int StringsFieldNumber = 3;
    private static readonly pb::FieldCodec<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> _repeated_strings_codec
        = pb::FieldCodec.ForMessage(26, global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData.Parser);
    private readonly pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> strings_ = new pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> Strings {
      get { return strings_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as NetworkStringListFullUpdate);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(NetworkStringListFullUpdate other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ListId != other.ListId) return false;
      if (Name != other.Name) return false;
      if(!strings_.Equals(other.strings_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ListId != 0) hash ^= ListId.GetHashCode();
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      hash ^= strings_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (ListId != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(ListId);
      }
      if (Name.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Name);
      }
      strings_.WriteTo(output, _repeated_strings_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ListId != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(ListId);
      }
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      size += strings_.CalculateSize(_repeated_strings_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(NetworkStringListFullUpdate other) {
      if (other == null) {
        return;
      }
      if (other.ListId != 0) {
        ListId = other.ListId;
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
      strings_.Add(other.strings_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            ListId = input.ReadUInt32();
            break;
          }
          case 18: {
            Name = input.ReadString();
            break;
          }
          case 26: {
            strings_.AddEntriesFrom(input, _repeated_strings_codec);
            break;
          }
        }
      }
    }

  }

  public sealed partial class NetworkStringListUpdate : pb::IMessage<NetworkStringListUpdate> {
    private static readonly pb::MessageParser<NetworkStringListUpdate> _parser = new pb::MessageParser<NetworkStringListUpdate>(() => new NetworkStringListUpdate());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<NetworkStringListUpdate> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SharpLife.Networking.Shared.Messages.NetworkStringLists.NetworkStringListUpdatesReflection.Descriptor.MessageTypes[4]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListUpdate() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListUpdate(NetworkStringListUpdate other) : this() {
      listId_ = other.listId_;
      strings_ = other.strings_.Clone();
      updates_ = other.updates_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public NetworkStringListUpdate Clone() {
      return new NetworkStringListUpdate(this);
    }

    /// <summary>Field number for the "list_id" field.</summary>
    public const int ListIdFieldNumber = 1;
    private uint listId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint ListId {
      get { return listId_; }
      set {
        listId_ = value;
      }
    }

    /// <summary>Field number for the "strings" field.</summary>
    public const int StringsFieldNumber = 2;
    private static readonly pb::FieldCodec<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> _repeated_strings_codec
        = pb::FieldCodec.ForMessage(18, global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData.Parser);
    private readonly pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> strings_ = new pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringData> Strings {
      get { return strings_; }
    }

    /// <summary>Field number for the "updates" field.</summary>
    public const int UpdatesFieldNumber = 3;
    private static readonly pb::FieldCodec<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate> _repeated_updates_codec
        = pb::FieldCodec.ForMessage(26, global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate.Parser);
    private readonly pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate> updates_ = new pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::SharpLife.Networking.Shared.Messages.NetworkStringLists.ListStringDataUpdate> Updates {
      get { return updates_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as NetworkStringListUpdate);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(NetworkStringListUpdate other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ListId != other.ListId) return false;
      if(!strings_.Equals(other.strings_)) return false;
      if(!updates_.Equals(other.updates_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ListId != 0) hash ^= ListId.GetHashCode();
      hash ^= strings_.GetHashCode();
      hash ^= updates_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (ListId != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(ListId);
      }
      strings_.WriteTo(output, _repeated_strings_codec);
      updates_.WriteTo(output, _repeated_updates_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ListId != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(ListId);
      }
      size += strings_.CalculateSize(_repeated_strings_codec);
      size += updates_.CalculateSize(_repeated_updates_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(NetworkStringListUpdate other) {
      if (other == null) {
        return;
      }
      if (other.ListId != 0) {
        ListId = other.ListId;
      }
      strings_.Add(other.strings_);
      updates_.Add(other.updates_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            ListId = input.ReadUInt32();
            break;
          }
          case 18: {
            strings_.AddEntriesFrom(input, _repeated_strings_codec);
            break;
          }
          case 26: {
            updates_.AddEntriesFrom(input, _repeated_updates_codec);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code

// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Module.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace ComposeVR.Protocol.Module
{

    /// <summary>Holder for reflection information generated from Module.proto</summary>
    public static partial class ModuleReflection
    {

        #region Descriptor
        /// <summary>File descriptor for Module.proto</summary>
        public static pbr::FileDescriptor Descriptor
        {
            get { return descriptor; }
        }
        private static pbr::FileDescriptor descriptor;

        static ModuleReflection()
        {
            byte[] descriptorData = global::System.Convert.FromBase64String(
                string.Concat(
                  "CgxNb2R1bGUucHJvdG8SCUNvbXBvc2VWUiImChFDcmVhdGVTb3VuZE1vZHVs",
                  "ZRIRCglzZW5kZXJfaWQYASABKAkiFgoUT25Tb3VuZE1vZHVsZUNyZWF0ZWQi",
                  "ZgoLT3BlbkJyb3dzZXISEwoLZGV2aWNlX3R5cGUYAiABKAkSFAoMY29udGVu",
                  "dF90eXBlGAMgASgJEhQKDGRldmljZV9pbmRleBgEIAEoBRIWCg5yZXBsYWNl",
                  "X2RldmljZRgFIAEoCCIYCghNSURJTm90ZRIMCgRNSURJGAEgASgMQjsKHWNv",
                  "bS5sYXM0dmMuY29tcG9zZXZyLnByb3RvY29sqgIZQ29tcG9zZVZSLlByb3Rv",
                  "Y29sLk1vZHVsZWIGcHJvdG8z"));
            descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
                new pbr::FileDescriptor[] { },
                new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::ComposeVR.Protocol.Module.CreateSoundModule), global::ComposeVR.Protocol.Module.CreateSoundModule.Parser, new[]{ "SenderId" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::ComposeVR.Protocol.Module.OnSoundModuleCreated), global::ComposeVR.Protocol.Module.OnSoundModuleCreated.Parser, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::ComposeVR.Protocol.Module.OpenBrowser), global::ComposeVR.Protocol.Module.OpenBrowser.Parser, new[]{ "DeviceType", "ContentType", "DeviceIndex", "ReplaceDevice" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::ComposeVR.Protocol.Module.MIDINote), global::ComposeVR.Protocol.Module.MIDINote.Parser, new[]{ "MIDI" }, null, null, null)
                }));
        }
        #endregion

    }
    #region Messages
    public sealed partial class CreateSoundModule : pb::IMessage<CreateSoundModule>
    {
        private static readonly pb::MessageParser<CreateSoundModule> _parser = new pb::MessageParser<CreateSoundModule>(() => new CreateSoundModule());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<CreateSoundModule> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor
        {
            get { return global::ComposeVR.Protocol.Module.ModuleReflection.Descriptor.MessageTypes[0]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public CreateSoundModule()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public CreateSoundModule(CreateSoundModule other) : this()
        {
            senderId_ = other.senderId_;
            _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public CreateSoundModule Clone()
        {
            return new CreateSoundModule(this);
        }

        /// <summary>Field number for the "sender_id" field.</summary>
        public const int SenderIdFieldNumber = 1;
        private string senderId_ = "";
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string SenderId
        {
            get { return senderId_; }
            set
            {
                senderId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as CreateSoundModule);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(CreateSoundModule other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (SenderId != other.SenderId) return false;
            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (SenderId.Length != 0) hash ^= SenderId.GetHashCode();
            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (SenderId.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(SenderId);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (SenderId.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(SenderId);
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(CreateSoundModule other)
        {
            if (other == null)
            {
                return;
            }
            if (other.SenderId.Length != 0)
            {
                SenderId = other.SenderId;
            }
            _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                    case 10:
                        {
                            SenderId = input.ReadString();
                            break;
                        }
                }
            }
        }

    }

    public sealed partial class OnSoundModuleCreated : pb::IMessage<OnSoundModuleCreated>
    {
        private static readonly pb::MessageParser<OnSoundModuleCreated> _parser = new pb::MessageParser<OnSoundModuleCreated>(() => new OnSoundModuleCreated());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<OnSoundModuleCreated> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor
        {
            get { return global::ComposeVR.Protocol.Module.ModuleReflection.Descriptor.MessageTypes[1]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OnSoundModuleCreated()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OnSoundModuleCreated(OnSoundModuleCreated other) : this()
        {
            _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OnSoundModuleCreated Clone()
        {
            return new OnSoundModuleCreated(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as OnSoundModuleCreated);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(OnSoundModuleCreated other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(OnSoundModuleCreated other)
        {
            if (other == null)
            {
                return;
            }
            _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                }
            }
        }

    }

    public sealed partial class OpenBrowser : pb::IMessage<OpenBrowser>
    {
        private static readonly pb::MessageParser<OpenBrowser> _parser = new pb::MessageParser<OpenBrowser>(() => new OpenBrowser());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<OpenBrowser> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor
        {
            get { return global::ComposeVR.Protocol.Module.ModuleReflection.Descriptor.MessageTypes[2]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OpenBrowser()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OpenBrowser(OpenBrowser other) : this()
        {
            deviceType_ = other.deviceType_;
            contentType_ = other.contentType_;
            deviceIndex_ = other.deviceIndex_;
            replaceDevice_ = other.replaceDevice_;
            _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public OpenBrowser Clone()
        {
            return new OpenBrowser(this);
        }

        /// <summary>Field number for the "device_type" field.</summary>
        public const int DeviceTypeFieldNumber = 2;
        private string deviceType_ = "";
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string DeviceType
        {
            get { return deviceType_; }
            set
            {
                deviceType_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        /// <summary>Field number for the "content_type" field.</summary>
        public const int ContentTypeFieldNumber = 3;
        private string contentType_ = "";
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string ContentType
        {
            get { return contentType_; }
            set
            {
                contentType_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        /// <summary>Field number for the "device_index" field.</summary>
        public const int DeviceIndexFieldNumber = 4;
        private int deviceIndex_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int DeviceIndex
        {
            get { return deviceIndex_; }
            set
            {
                deviceIndex_ = value;
            }
        }

        /// <summary>Field number for the "replace_device" field.</summary>
        public const int ReplaceDeviceFieldNumber = 5;
        private bool replaceDevice_;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool ReplaceDevice
        {
            get { return replaceDevice_; }
            set
            {
                replaceDevice_ = value;
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as OpenBrowser);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(OpenBrowser other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (DeviceType != other.DeviceType) return false;
            if (ContentType != other.ContentType) return false;
            if (DeviceIndex != other.DeviceIndex) return false;
            if (ReplaceDevice != other.ReplaceDevice) return false;
            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (DeviceType.Length != 0) hash ^= DeviceType.GetHashCode();
            if (ContentType.Length != 0) hash ^= ContentType.GetHashCode();
            if (DeviceIndex != 0) hash ^= DeviceIndex.GetHashCode();
            if (ReplaceDevice != false) hash ^= ReplaceDevice.GetHashCode();
            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (DeviceType.Length != 0)
            {
                output.WriteRawTag(18);
                output.WriteString(DeviceType);
            }
            if (ContentType.Length != 0)
            {
                output.WriteRawTag(26);
                output.WriteString(ContentType);
            }
            if (DeviceIndex != 0)
            {
                output.WriteRawTag(32);
                output.WriteInt32(DeviceIndex);
            }
            if (ReplaceDevice != false)
            {
                output.WriteRawTag(40);
                output.WriteBool(ReplaceDevice);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (DeviceType.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceType);
            }
            if (ContentType.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeStringSize(ContentType);
            }
            if (DeviceIndex != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeInt32Size(DeviceIndex);
            }
            if (ReplaceDevice != false)
            {
                size += 1 + 1;
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(OpenBrowser other)
        {
            if (other == null)
            {
                return;
            }
            if (other.DeviceType.Length != 0)
            {
                DeviceType = other.DeviceType;
            }
            if (other.ContentType.Length != 0)
            {
                ContentType = other.ContentType;
            }
            if (other.DeviceIndex != 0)
            {
                DeviceIndex = other.DeviceIndex;
            }
            if (other.ReplaceDevice != false)
            {
                ReplaceDevice = other.ReplaceDevice;
            }
            _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                    case 18:
                        {
                            DeviceType = input.ReadString();
                            break;
                        }
                    case 26:
                        {
                            ContentType = input.ReadString();
                            break;
                        }
                    case 32:
                        {
                            DeviceIndex = input.ReadInt32();
                            break;
                        }
                    case 40:
                        {
                            ReplaceDevice = input.ReadBool();
                            break;
                        }
                }
            }
        }

    }

    public sealed partial class MIDINote : pb::IMessage<MIDINote>
    {
        private static readonly pb::MessageParser<MIDINote> _parser = new pb::MessageParser<MIDINote>(() => new MIDINote());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<MIDINote> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor
        {
            get { return global::ComposeVR.Protocol.Module.ModuleReflection.Descriptor.MessageTypes[3]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public MIDINote()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public MIDINote(MIDINote other) : this()
        {
            mIDI_ = other.mIDI_;
            _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public MIDINote Clone()
        {
            return new MIDINote(this);
        }

        /// <summary>Field number for the "MIDI" field.</summary>
        public const int MIDIFieldNumber = 1;
        private pb::ByteString mIDI_ = pb::ByteString.Empty;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public pb::ByteString MIDI
        {
            get { return mIDI_; }
            set
            {
                mIDI_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as MIDINote);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(MIDINote other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (MIDI != other.MIDI) return false;
            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            int hash = 1;
            if (MIDI.Length != 0) hash ^= MIDI.GetHashCode();
            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return pb::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output)
        {
            if (MIDI.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteBytes(MIDI);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            int size = 0;
            if (MIDI.Length != 0)
            {
                size += 1 + pb::CodedOutputStream.ComputeBytesSize(MIDI);
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(MIDINote other)
        {
            if (other == null)
            {
                return;
            }
            if (other.MIDI.Length != 0)
            {
                MIDI = other.MIDI;
            }
            _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                    case 10:
                        {
                            MIDI = input.ReadBytes();
                            break;
                        }
                }
            }
        }

    }

    #endregion

}

#endregion Designer generated code

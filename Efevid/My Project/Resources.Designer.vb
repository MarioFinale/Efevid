﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace My.Resources

    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0"),
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),
     Global.Microsoft.VisualBasic.HideModuleNameAttribute()>
    Public Module Resources

        Private resourceMan As Global.System.Resources.ResourceManager

        Private resourceCulture As Global.System.Globalization.CultureInfo

        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>
        Public ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("Efevid.Resources", GetType(Resources).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property

        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>
        Public Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = Value
            End Set
        End Property

        '''<summary>
        '''  Looks up a localized string similar to &lt;html&gt;&lt;meta http-equiv=&quot;refresh&quot; content=&quot;1&quot;&gt;&lt;div align=&quot;center&quot;&gt;&lt;h1&gt;GENERANDO VIDEOS&lt;/h1&gt;&lt;/div&gt;&lt;/html&gt;.
        '''</summary>
        Public ReadOnly Property status_gen() As String
            Get
                Return ResourceManager.GetString("status_gen", resourceCulture)
            End Get
        End Property

        '''<summary>
        '''  Looks up a localized string similar to &lt;html&gt;&lt;meta http-equiv=&quot;refresh&quot; content=&quot;1&quot;&gt;&lt;div align=&quot;center&quot;&gt;&lt;h1&gt;CARGANDO...&lt;/h1&gt;&lt;/div&gt;&lt;/html&gt;.
        '''</summary>
        Public ReadOnly Property status_loading() As String
            Get
                Return ResourceManager.GetString("status_loading", resourceCulture)
            End Get
        End Property

        '''<summary>
        '''  Looks up a localized string similar to &lt;html&gt;&lt;meta http-equiv=&quot;refresh&quot; content=&quot;1&quot;&gt;&lt;div align=&quot;center&quot;&gt;&lt;h1&gt;VIDEOS GENERADOS&lt;/h1&gt;&lt;/div&gt;&lt;/html&gt;.
        '''</summary>
        Public ReadOnly Property status_OK() As String
            Get
                Return ResourceManager.GetString("status_OK", resourceCulture)
            End Get
        End Property
    End Module
End Namespace

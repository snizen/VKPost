Imports System
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Collections
Imports Newtonsoft.Json

Module Module1

    Public Class MultipartForm

        Public Cookies As CookieContainer
        Private coFormFields As Hashtable
        Protected coRequest As HttpWebRequest
        Protected coFileStream As System.IO.Stream
        Dim CONTENT_DISP As String = "Content-Disposition: form-data; name="
        Public TransferHttpVersion As Version
        Public FileContentType As String

        Public Sub New(ByVal _url As String)
            URL = _url
            coFormFields = New Hashtable()
            ResponseText = New StringBuilder()
            BufferSize = 1024 * 10
            BeginBoundary = "BoundarySepR8R"
            TransferHttpVersion = HttpVersion.Version11
            FileContentType = "text/xml"
        End Sub

        Dim _BeginBoundary As String

        Public Property BeginBoundary() As String
            Get
                Return _BeginBoundary
            End Get
            Set(ByVal Value As String)
                _BeginBoundary = Value
                ContentBoundary = "--" + BeginBoundary
                EndingBoundary = ContentBoundary + "--"
            End Set
        End Property

        Protected ContentBoundary As String
        Protected EndingBoundary As String
        Public ResponseText As StringBuilder
        Public URL As String
        Public BufferSize As Integer

        Public Sub setFilename(ByVal path As String)
            coFileStream = New System.IO.FileStream(path, FileMode.Create, FileAccess.Write)
        End Sub

        Public Sub setField(ByVal key As String, ByVal str As String)
            coFormFields.Add(key, str)
        End Sub

        Public Function getStream() As System.IO.Stream
            Dim io As System.IO.Stream
            If (coFileStream Is Nothing) Then
                io = coRequest.GetRequestStream()
            Else
                io = coFileStream
            End If
            Return io
        End Function

        Public Sub getResponse()
            If (coFileStream Is Nothing) Then
                Dim io As System.IO.Stream
                Dim oResponse As WebResponse
                Try
                    oResponse = coRequest.GetResponse()
                Catch web As WebException
                    oResponse = web.Response
                End Try
                If (Not (oResponse Is Nothing)) Then
                    io = oResponse.GetResponseStream()
                    Dim sr As StreamReader = New StreamReader(io)
                    Dim str As String
                    ResponseText.Length = 0
                    str = sr.ReadLine()
                    While (Not (str Is Nothing))
                        ResponseText.Append(str)
                        str = sr.ReadLine()
                    End While
                    oResponse.Close()
                Else
                    Throw New Exception("MultipartForm: Error retrieving server response")
                End If
            End If
        End Sub

        Public Sub sendFile(ByVal aFilename As String)
            coRequest = WebRequest.Create(URL)
            coRequest.CookieContainer = Cookies
            '// Set use HTTP 1.0 or 1.1.
            coRequest.ProtocolVersion = TransferHttpVersion
            coRequest.Method = "POST"
            coRequest.ContentType = "multipart/form-data, boundary=" + BeginBoundary
            coRequest.Headers.Add("Cache-Control", "no-cache")
            coRequest.KeepAlive = True
            Dim strFields As String = getFormfields()
            Dim strFileHdr As String = getFileheader(aFilename)
            Dim strFileTlr As String = getFiletrailer()
            Dim info As FileInfo = New FileInfo(aFilename)
            coRequest.ContentLength = strFields.Length + strFileHdr.Length +
            strFileTlr.Length + info.Length
            Dim io As System.IO.Stream
            io = getStream()
            writeString(io, strFields)
            writeString(io, strFileHdr)
            writeFile(io, aFilename)
            writeString(io, strFileTlr)
            io.Close()
            'MsgBox(coRequest.Headers.ToString)
            getResponse()
            '// End the life time of this request object.
            coRequest = Nothing
        End Sub
        Public Sub writeString(ByVal io As System.IO.Stream, ByVal str As String)
            Dim PostData As Byte() = System.Text.Encoding.ASCII.GetBytes(str)
            io.Write(PostData, 0, PostData.Length)
        End Sub

        Public Function getFormfields() As String
            Dim str As String = ""
            Dim myEnumerator As IDictionaryEnumerator = coFormFields.GetEnumerator()
            While (myEnumerator.MoveNext())
                str += ContentBoundary + vbCrLf + CONTENT_DISP + """" + myEnumerator.Key +
                """" + vbCrLf + vbCrLf + myEnumerator.Value + vbCrLf
            End While
            Return str
        End Function
        Public Function getFileheader(ByVal aFilename As String) As String
            Return ContentBoundary + vbCrLf + CONTENT_DISP + """userfile""; name=""photo""; filename=""" + Path.GetFileName(aFilename) + """" + vbCrLf + "Content-type: " + FileContentType + vbCrLf + vbCrLf
        End Function

        Public Function getFiletrailer() As String
            Return vbCrLf + EndingBoundary
        End Function

        Public Sub writeFile(ByVal io As System.IO.Stream, ByVal aFilename As String)
            Dim readIn As FileStream = New FileStream(aFilename, FileMode.Open,
            FileAccess.Read)
            readIn.Seek(0, SeekOrigin.Begin) ' move to the start of the file
            Dim fileData(BufferSize) As Byte
            Dim bytes As Integer
            bytes = readIn.Read(fileData, 0, BufferSize)
            While (bytes > 0)
                '// read the file data and send a chunk at a time
                io.Write(fileData, 0, bytes)
                bytes = readIn.Read(fileData, 0, BufferSize)
            End While
            readIn.Close()
        End Sub

    End Class

End Module
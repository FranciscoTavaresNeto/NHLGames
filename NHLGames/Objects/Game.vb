﻿Imports System.Globalization
Imports Newtonsoft.Json.Linq
Imports NHLGames.My.Resources
Imports NHLGames.Utilities

Namespace Objects
    <DebuggerDisplay("{HomeTeam} vs. {AwayTeam} at {[Date]}")>
    Public NotInheritable Class Game
        Implements IDisposable
        Private _disposedValue As Boolean

        Public Property StreamsDict As Dictionary(Of StreamTypeEnum, GameStream)
        Public Property StreamsUnknown As List(Of GameStream)
        Public Property GameId As String
        Public Property GameType As GameTypeEnum 'Get type of the game : 1 preseason, 2 regular, 3 series
        Public Property GameDate As DateTime
        Public Property GameState As GameStateEnum
        Public Property GameStateDetailed As String

        Public Property GamePeriod As String '1st 2nd 3rd OT SO OT2..
        Public Property GameTimeLeft As String 'Final, 12:34, 20:00
        Public Property IsInIntermission As Boolean
        Public Property IntermissionTimeRemaining As Date 'seconds

        Public Property SeriesGameNumber As String 'Series: Game 1.. 7
        Public Property SeriesGameStatus As String 'Series: Team wins 4-2, Tied 2-2, Team leads 1-0

        Public Property Away As String
        Public Property AwayAbbrev As String
        Public Property AwayTeam As String
        Public Property AwayScore As String

        Public Property Home As String
        Public Property HomeAbbrev As String
        Public Property HomeTeam As String
        Public Property HomeScore As String

        Public Overrides Function ToString() As String
            Return String.Format(NHLGamesMetro.RmText.GetString("msgTeamVsTeam"), HomeTeam, AwayTeam)
        End Function

        Public ReadOnly Property IsLive As Boolean
            Get
                Return GameState.Equals(GameStateEnum.InProgress) OrElse
                       GameState.Equals(GameStateEnum.Ending) OrElse
                       GameState.Equals(GameStateEnum.Ended) OrElse
                       GameState.Equals(GameStateEnum.OffTheAir)
            End Get
        End Property

        Public ReadOnly Property IsOffTheAir As Boolean
            Get
                Return GameState.Equals(GameStateEnum.OffTheAir) OrElse
                       GameState.Equals(GameStateEnum.StreamEnded)
            End Get
        End Property

        Public ReadOnly Property IsUnplayable As Boolean
            Get
                Return GameState > GameStateEnum.StreamEnded OrElse GameState = GameStateEnum.Undefined
            End Get
        End Property

        Public ReadOnly Property IsStreamable As Boolean
            Get
                Return GameState > GameStateEnum.Pregame AndAlso GameState <= GameStateEnum.StreamEnded
            End Get
        End Property

        Public ReadOnly Property AreAnyStreamsAvailable As Boolean
            Get
                Return ((StreamsDict IsNot Nothing AndAlso StreamsDict.Count > 0) OrElse (StreamsUnknown IsNot Nothing AndAlso StreamsUnknown.Count > 0)) AndAlso Not IsUnplayable
            End Get
        End Property

        Public Function GetStream(streamType As StreamTypeEnum) As GameStream
            Return If(StreamsDict IsNot Nothing,
                       StreamsDict.FirstOrDefault(Function(x) x.Key = streamType).Value,
                       Nothing)
        End Function

        Public Function IsStreamDefined(streamType As StreamTypeEnum) As Boolean
            Return (StreamsDict IsNot Nothing) AndAlso StreamsDict.ContainsKey(streamType)
        End Function

        Public Sub SetGameDate(jDate As String)
            Dim dateTimeVal As Date

            If (Date.TryParseExact(jDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None,
                                        dateTimeVal) = False) Then
                dateTimeVal = Date.Parse(jDate)
            End If

            GameDate = dateTimeVal.ToUniversalTime() ' Must use universal time to always get correct date for stream
        End Sub

        Public Function SetSeriesInfo(game As JObject) As Boolean
            If Not game.TryGetValue("seriesSummary", "gameNumber") And
               game.TryGetValue("seriesSummary", "seriesStatusShort") Then
                Console.WriteLine(English.errorUnableToDecodeJson)
                Return False
            End If

            SeriesGameNumber = game.SelectToken("seriesSummary.gameNumber").ToString()
            SeriesGameStatus = game.SelectToken("seriesSummary.seriesStatusShort").ToString()
            Return True
        End Function

        Public Sub SetStatsInfo(game As JObject)
            HomeScore = game.SelectToken("teams.home.score").ToString()
            AwayScore = game.SelectToken("teams.away.score").ToString()
            GamePeriod = game.SelectToken("linescore.currentPeriodOrdinal").ToString()
            GameTimeLeft = game.SelectToken("linescore.currentPeriodTimeRemaining").ToString()
            IsInIntermission =
                game.SelectToken("linescore.intermissionInfo.inIntermission").ToString().ToLower().Equals("true")

            If IsInIntermission Then
                IntermissionTimeRemaining =
                    Date.MinValue.AddSeconds(
                        CType(game.SelectToken("linescore.intermissionInfo.intermissionTimeRemaining").ToString(),
                              Integer))
            End If
        End Sub

        Public Sub New()
            StreamsDict = New Dictionary(Of StreamTypeEnum, GameStream)()
            StreamsUnknown = New List(Of GameStream)()
        End Sub

        Private Sub Dispose(disposing As Boolean)
            If Not _disposedValue Then
                _disposedValue = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub
    End Class
End Namespace

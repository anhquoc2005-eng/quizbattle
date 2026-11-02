/** Tên method Client -> Server: gọi thẳng qua connection.invoke/send(...) - khớp tên method C# của GameHub. */
export const GAME_HUB_METHODS = {
  JOIN_ROOM: 'JoinRoom',
  START_GAME: 'StartGame',
  END_QUESTION: 'EndQuestion',
  NEXT_QUESTION: 'NextQuestion',
  SUBMIT_ANSWER: 'SubmitAnswer',
  LEAVE_ROOM: 'LeaveRoom',
} as const

/** Tên sự kiện Server -> Client, khớp GameHubEvents.cs. */
export const GAME_HUB_EVENTS = {
  ROOM_STATE: 'RoomState',
  PLAYER_JOINED: 'PlayerJoined',
  PLAYER_LEFT: 'PlayerLeft',
  QUESTION_STARTED: 'QuestionStarted',
  QUESTION_ENDED: 'QuestionEnded',
  LEADERBOARD_UPDATED: 'LeaderboardUpdated',
  GAME_ENDED: 'GameEnded',
  ANSWER_PROGRESS: 'AnswerProgress',
  ERROR: 'Error',
} as const

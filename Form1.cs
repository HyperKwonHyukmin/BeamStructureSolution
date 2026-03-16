using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BeamStructureSolution.Control;
using BeamStructureSolution.Utils;
using BeamStructureSolution.Model;


namespace BeamStructureSolution
{
  public partial class Form1 : Form
  {
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    private bool isBeamModelVisible = false;
    // BDF 파일이 저장되는 경로 지정
    private string selectedFolderPath = @"C:\temp\BeamStructureAnalysis";
    private BeamStructureSolution.Control.FeModelGenerator modelGenerator;
    private string f06FilePath;



    // Position 박스들 (Load, Boundary)
    TextBox[] loadPositionBoxes;
    TextBox[] bcPositionBoxes;


    // 이전 값 저장용
    private Dictionary<TextBox, string> previousValues = new();

    // 해석 결과 저장용
    private List<(double pos, double startShear, double endShear)> shearResult;
    private List<(double pos, double startMoment, double endMoment)> momentResult;
    private (double? Area, double? I) sectionProperty = (null, null); // 단면 특성 초기화
    private (int eleID, double stress) maxStressItem = (0, 0.0); // 최대 응력 초기화
    private (int eleID, double displacement) maxDisplacement = (0, 0.0); // 최대 변위 초기화

    private List<(double pos, string constraint)> boundaryConditions = new();
    private List<(double pos, double mag)> loads = new();
    private double length;


    public Form1()
    {

      InitializeComponent();
      this.Load += Form1_Load;
      this.Shown += Form1_Shown;
      this.Text = "HiTESS BeamStrucureSolution";
      this.StartPosition = FormStartPosition.Manual;   // 수동 위치 지정
      this.Location = new Point(50, 50);             // 화면의 (x=100, y=100) 위치에 띄움

      //// 디버깅용 인풋
      //// Beam 기본값
      //lengthBox.Text = "1000";
      //dim1Box.Text = "10";
      //dim2Box.Text = "20";

      //// Boundary Position
      //bcPosition1Box.Text = "0";
      //bcPosition2Box.Text = "300";
      //bcPosition3Box.Text = "700";
      //bcPosition4Box.Text = "1000";

      //// Boundary 조건
      //bcPosition1Combo.SelectedItem = "Fix";
      //bcPosition2Combo.SelectedItem = "Hinge";
      //bcPosition3Combo.SelectedItem = "Hinge";
      //bcPosition4Combo.SelectedItem = "Fix";

      //// Load Position
      //LoadPosition1Box.Text = "500";

      //// Load Magnitude
      //LoadMag1Box.Text = "100";

      //AllocConsole(); // 콘솔 강제 생성
      runButton.Enabled = false; // 초기 상태에서 비활성화
      resultButton.Enabled = false;
      showChartButton.Enabled = false;

      beamPreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;  // 이미지 크기 조절 방식
      beamPreviewPictureBox.Image = Resource1.BAR;               // 기본 이미지 표시

      // 콤보박스 가운데 정렬 설정
      // 가운데 정렬을 위한 설정
      beamTypeComboBox.DrawMode = DrawMode.OwnerDrawFixed;
      beamTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      beamTypeComboBox.DrawItem += ComboBox_DrawItem;

      // 모든 Boundary ComboBox 가운데 정렬 적용
      ComboBox[] constraintCombos = {bcPosition1Combo, bcPosition2Combo, bcPosition3Combo, bcPosition4Combo, bcPosition5Combo
};

      foreach (var cb in constraintCombos)
      {
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        cb.DrawItem += ComboBox_DrawItem;
      }

      beamTypeComboBox.Items.AddRange(new string[]
      {
        "BAR", "L", "T", "H", "CHAN", "ROD", "TUBE"
      });

      beamTypeComboBox.SelectedItem = "BAR"; // 선택 상태 반영
      beamTypeComboBox.SelectedIndexChanged += beamTypeComboBox_SelectedIndexChanged;

      runButton.Click += runButton_Click;
      resultButton.Click += resultButton_Click;
      LogInButton.Click += loginButton_Click;
      JoinButton.Click += joinButton_Click;

      TextBox[] allNumericBoxes = {
        lengthBox, dim1Box, dim2Box, dim3Box, dim4Box,
        bcPosition2Box, bcPosition3Box, bcPosition4Box, bcPosition5Box, bcPosition1Box,
        LoadPosition1Box, LoadPosition2Box, LoadPosition3Box, LoadPosition4Box, LoadPosition5Box,
        LoadMag1Box, LoadMag2Box, LoadMag3Box, LoadMag4Box, LoadMag5Box };


      foreach (TextBox tb in allNumericBoxes)
      {
        tb.KeyPress += OnlyAllowDecimalInput;
      }

      // Position TextBox 배열 정의
      loadPositionBoxes = new[] {
        LoadPosition1Box, LoadPosition2Box, LoadPosition3Box, LoadPosition4Box, LoadPosition5Box
    };

      bcPositionBoxes = new[] {
        bcPosition1Box, bcPosition2Box, bcPosition3Box, bcPosition4Box, bcPosition5Box
    };

      // [여기에 작성!] Leave 이벤트 연결 + 초기값 저장
      foreach (var tb in loadPositionBoxes.Concat(bcPositionBoxes))
      {
        tb.Leave += PositionBox_ValidateOnLeave;
        previousValues[tb] = tb.Text;
      }
    }

    private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      ComboBox combo = sender as ComboBox;

      // 아이템 유효성 검사
      if (e.Index < 0) return;

      // 배경 및 기본 텍스트 색
      e.DrawBackground();
      string text = combo.Items[e.Index].ToString();

      using (StringFormat sf = new StringFormat())
      {
        sf.Alignment = StringAlignment.Center;         // 수평 가운데
        sf.LineAlignment = StringAlignment.Center;     // 수직 가운데

        using (Brush textBrush = new SolidBrush(e.ForeColor))
        {
          e.Graphics.DrawString(text, e.Font, textBrush, e.Bounds, sf);
        }
      }

      e.DrawFocusRectangle();
    }

    private void beamTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
      string selected = beamTypeComboBox.SelectedItem?.ToString() ?? "";
      lengthBox.Text = "";
      dim1Box.Text = "";
      dim2Box.Text = "";
      dim3Box.Text = "";
      dim4Box.Text = "";

      switch (selected)
      {
        case "BAR":
          beamPreviewPictureBox.Image = Resource1.BAR;
          break;
        case "L":
          beamPreviewPictureBox.Image = Resource1.L;
          break;
        case "T":
          beamPreviewPictureBox.Image = Resource1.T;
          break;
        case "H":
          beamPreviewPictureBox.Image = Resource1.H;
          break;
        case "CHAN":
          beamPreviewPictureBox.Image = Resource1.CHAN;
          break;
        case "ROD":
          beamPreviewPictureBox.Image = Resource1.ROD;
          break;
        case "TUBE":
          beamPreviewPictureBox.Image = Resource1.TUBE;
          break;
        default:
          beamPreviewPictureBox.Image = null;
          break;
      }
    }
   

    public void UpdateStatus(string message)
    {
      statusText.Text = $"[진행 상황] {message}";
      statusText.Refresh(); // 강제로 UI 갱신
    }

    private void OnlyAllowDecimalInput(object sender, KeyPressEventArgs e)
    {
      TextBox tb = sender as TextBox;

      // LoadMagBox 계열은 음수 허용
      bool isLoadMagBox = tb == LoadMag1Box || tb == LoadMag2Box ||
                          tb == LoadMag3Box || tb == LoadMag4Box || tb == LoadMag5Box;

      if (isLoadMagBox)
      {
        // 숫자, 소수점, 음수 부호, 백스페이스만 허용
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
        {
          e.Handled = true;
        }

        // 소수점 중복 방지
        if (e.KeyChar == '.' && tb.Text.Contains("."))
        {
          e.Handled = true;
        }

        // 음수 부호는 맨 앞에서만 허용
        if (e.KeyChar == '-' && (tb.SelectionStart != 0 || tb.Text.Contains("-")))
        {
          e.Handled = true;
        }
      }
      else
      {
        // 나머지 박스는 양 실수만 허용
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
        {
          e.Handled = true;
        }

        if (e.KeyChar == '.' && tb.Text.Contains("."))
        {
          e.Handled = true;
        }
      }
    }

    private void PositionBox_ValidateOnLeave(object sender, EventArgs e)
    {
      TextBox currentBox = sender as TextBox;

      if (!double.TryParse(currentBox.Text, out double currentValue))
      {
        // 유효하지 않은 숫자일 경우 현재 값 저장 (사실상 무시)
        previousValues[currentBox] = currentBox.Text;
        return;
      }

      // Length 확인
      if (!double.TryParse(lengthBox.Text, out double length))
        return;

      if (currentValue > length)
      {
        MessageBox.Show("Position 값은 Length보다 클 수 없습니다!", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        if (previousValues.TryGetValue(currentBox, out string prev))
        {
          currentBox.Text = prev;
          currentBox.SelectionStart = currentBox.Text.Length;
        }

        return;
      }

      // 유효하면 현재 값을 이전값으로 저장
      previousValues[currentBox] = currentBox.Text;
    }

    private void BeamTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      string selectedType = beamTypeComboBox.SelectedItem.ToString();

      // 예: 모든 DIM 박스 초기화
      dim1Box.Enabled = true;
      dim2Box.Enabled = true;
      dim3Box.Enabled = true;
      dim4Box.Enabled = true;

      // 조건에 따라 비활성화
      if (selectedType == "BAR")
      {
        dim3Box.Enabled = false;
        dim4Box.Enabled = false;
      }
      else if (selectedType == "ROD")
      {
        dim2Box.Enabled = false;
        dim3Box.Enabled = false;
        dim4Box.Enabled = false;
      }
      else if (selectedType == "TUBE")
      {
        dim3Box.Enabled = false;
        dim4Box.Enabled = false;
      }
      // CHANNEL 등은 모두 활성화 유지
    }

    private async void runButton_Click(object sender, EventArgs e)
    {
      try
      {
        if (Directory.Exists(selectedFolderPath))
        {
          var files = Directory.GetFiles(selectedFolderPath);
          foreach (var file in files)
          {
            File.Delete(file);
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"기존 파일 삭제 중 오류 발생: {ex.Message}");
        return; // 삭제 실패 시 실행 중단
      }

      UpdateStatus("서버에서 구조 해석을 진행 중입니다...");
      resultButton.Enabled = false;

      string beamType = beamTypeComboBox.SelectedItem?.ToString() ?? "";

      // Beam Type별 필수 입력 필드 지정
      Dictionary<string, TextBox[]> requiredFieldsByBeamType = new()
    {
        { "BAR", new[] { lengthBox, dim1Box, dim2Box } },
        { "L",   new[] { lengthBox, dim1Box, dim2Box, dim3Box, dim4Box } },
        { "T",   new[] { lengthBox, dim1Box, dim2Box, dim3Box, dim4Box } },
        { "H",   new[] { lengthBox, dim1Box, dim2Box, dim3Box, dim4Box } },
        { "CHAN", new[] { lengthBox, dim1Box, dim2Box, dim3Box, dim4Box } },
        { "ROD",  new[] { lengthBox, dim1Box } },
        { "TUBE", new[] { lengthBox, dim1Box, dim2Box } },
    };

      // 필수 입력값 체크
      if (requiredFieldsByBeamType.TryGetValue(beamType, out TextBox[] requiredFields))
      {
        foreach (var tb in requiredFields)
        {
          if (string.IsNullOrWhiteSpace(tb.Text))
          {
            tb.BackColor = Color.LightPink;
            MessageBox.Show($"입력 누락: {tb.Name} 값이 필요합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tb.Focus();
            return;
          }
          else
          {
            tb.BackColor = Color.White;
          }
        }
      }

      // Beam 정보 수집
      double.TryParse(lengthBox.Text, out double length);
      double.TryParse(dim1Box.Text, out double dim1);
      double.TryParse(dim2Box.Text, out double dim2);
      double.TryParse(dim3Box.Text, out double dim3);
      double.TryParse(dim4Box.Text, out double dim4);

      List<double> dimList = new List<double>();
      dimList.Add(dim1);
      dimList.Add(dim2);
      dimList.Add(dim3);
      dimList.Add(dim4);

      // Boundary 조건 수집
      var boundaryConditions = new List<(double pos, string constraint)>();
      TextBox[] bcPos = { bcPosition1Box, bcPosition2Box, bcPosition3Box, bcPosition4Box, bcPosition5Box };
      ComboBox[] bcTypes = { bcPosition1Combo, bcPosition2Combo, bcPosition3Combo, bcPosition4Combo, bcPosition5Combo };

      for (int i = 0; i < 5; i++)
      {
        if (double.TryParse(bcPos[i].Text, out double pos))
        {
          string constraint = bcTypes[i].SelectedItem?.ToString() ?? "";
          if (!string.IsNullOrWhiteSpace(constraint))
            boundaryConditions.Add((pos, constraint));
        }
      }

      // Load 조건 수집
      var loads = new List<(double pos, double mag)>();
      TextBox[] loadPos = { LoadPosition1Box, LoadPosition2Box, LoadPosition3Box, LoadPosition4Box, LoadPosition5Box };
      TextBox[] loadMag = { LoadMag1Box, LoadMag2Box, LoadMag3Box, LoadMag4Box, LoadMag5Box };

      for (int i = 0; i < 5; i++)
      {
        if (double.TryParse(loadPos[i].Text, out double pos) &&
            double.TryParse(loadMag[i].Text, out double mag))
        {
          loads.Add((pos, mag));
        }
      }
      var beamSolutionRun = new BeamStructureSolutionRun(
        beamType, length, dimList, boundaryConditions, loads, selectedFolderPath);

      // 상태 업데이트 델리게이트 연결
      beamSolutionRun.OnStatusUpdate = (msg) =>
      {
        // UI 스레드에서 안전하게 실행
        if (InvokeRequired)
          Invoke(() => UpdateStatus(msg));
        else
          UpdateStatus(msg);
      };

      // 버튼 비활성화
      runButton.Enabled = false;

      // 튜플로 결과 받기
      //(shearResult, momentResult, sectionProperty, maxStressItem, maxDisplacement) = await Task.Run(() => beamSolutionRun.Run());
      //(string bdffullPath, BeamStructureSolution.Control.FeModelGenerator Generator) = beamSolutionRun.Run();
      (string bdffullPath, BeamStructureSolution.Control.FeModelGenerator Generator) = await Task.Run(() =>
      {
        return beamSolutionRun.Run();
      });

      modelGenerator = Generator;
      f06FilePath = bdffullPath.Replace(".bdf", ".f06");

      // 3. HTTP 클라이언트 설정
      using (var client = new HttpClient())
      using (var multipart = new MultipartFormDataContent())
      {
        // 4. BDF 파일을 multipart로 첨부
        var fileContent = new ByteArrayContent(File.ReadAllBytes(bdffullPath));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        multipart.Add(fileContent, "file", Path.GetFileName(bdffullPath));

        // BeamStructureSolutionUser.txt 파일을 multipart로 첨부
        string userFilePath = @"C:\temp\BeamStructureSolutionUser.txt";
        if (File.Exists(userFilePath))
        {
          var userFileContent = new ByteArrayContent(File.ReadAllBytes(userFilePath));
          userFileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
          multipart.Add(userFileContent, "userFile", Path.GetFileName(userFilePath));
        }

        // 5. POST 요청 전송
        string requestUrl = "http://10.14.42.145:9091/hitessbeam/beamStructureAnalysis";
        var response = await client.PostAsync(requestUrl, multipart);

        if (!response.IsSuccessStatusCode)
        {
          string errorMsg = await response.Content.ReadAsStringAsync();
          MessageBox.Show($"서버 오류 발생: {response.StatusCode}\n내용: {errorMsg}");
          UpdateStatus("구조 해석 실패!");
          return;
        }

        //// f06 파일 수신 및 저장
        byte[] f06Bytes = await response.Content.ReadAsByteArrayAsync();

        File.WriteAllBytes(f06FilePath, f06Bytes);
        MessageBox.Show("구조해석 완료! '해석 결과 확인' 버튼을 클릭해주세요");
      }
      // 완료 후
      UpdateStatus("구조 해석 완료!");
      runButton.Enabled = true;
      resultButton.Enabled = true;
    }

    private void resultButton_Click(object sender, EventArgs e)
    {
      //결과 저장용
      var resultShear = new List<(double pos, double startShear, double endShear)>();
      var resultMoment = new List<(double pos, double startMoment, double endMoment)>();
      (double? Area, double? I) sectionProperty = (null, null); // 단면 특성 초기화
      (int eleID, double stress) maxStressItem = (0, 0.0); // 최대 응력 초기화
      (int eleID, double displacement) maxDisplacement = (0, 0.0); // 최대 변위 초기화


      (shearResult, momentResult, sectionProperty, maxStressItem, maxDisplacement) =
          BeamStructureSolutionRun.AnalysisResultProcessor(f06FilePath, modelGenerator.nodeInstance);


      if (shearResult == null || momentResult == null || shearResult.Count == 0 || momentResult.Count == 0)
      {
        MessageBox.Show("해석 결과가 없습니다. 먼저 구조 해석을 수행해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      hasShearResult = true;
      hasMomentResult = true;

      // 새 결과로 다시 그림
      shearForceBox.Invalidate();
      bendingMomentBox.Invalidate();

      Inertia.Text = sectionProperty.I?.ToString("F1") ?? "N/A";
      Area.Text = sectionProperty.Area?.ToString("F1") ?? "N/A";
      MaxStress.Text = $"{maxStressItem.stress:F1}";
      MaxDis.Text = $"{maxDisplacement.displacement:F1}";
    }

    private void ShowChartButton_Click(object sender, EventArgs e)
    {
      double.TryParse(lengthBox.Text, out length);

      boundaryConditions.Clear();
      TextBox[] bcPos = { bcPosition1Box, bcPosition2Box, bcPosition3Box, bcPosition4Box, bcPosition5Box };
      ComboBox[] bcTypes = { bcPosition1Combo, bcPosition2Combo, bcPosition3Combo, bcPosition4Combo, bcPosition5Combo };

      for (int i = 0; i < 5; i++)
      {
        if (double.TryParse(bcPos[i].Text, out double pos))
        {
          string constraint = bcTypes[i].SelectedItem?.ToString() ?? "";
          if (!string.IsNullOrWhiteSpace(constraint))
            boundaryConditions.Add((pos, constraint));
        }
      }

      loads.Clear();
      TextBox[] loadPos = { LoadPosition1Box, LoadPosition2Box, LoadPosition3Box, LoadPosition4Box, LoadPosition5Box };
      TextBox[] loadMag = { LoadMag1Box, LoadMag2Box, LoadMag3Box, LoadMag4Box, LoadMag5Box };

      for (int i = 0; i < 5; i++)
      {
        if (double.TryParse(loadPos[i].Text, out double pos) &&
            double.TryParse(loadMag[i].Text, out double mag))
        {
          loads.Add((pos, mag));
        }
      }

      isBeamModelVisible = true; // 버튼 클릭하면 플래그 ON
      beamModelPanel.Invalidate();
    }

    private async void loginButton_Click(object sender, EventArgs e)
    {
      string userId = UserIdBox.Text.Trim();
      string company = "HD 현대중공업"; // 회사명은 필요시 입력란으로 변경 가능

      if (string.IsNullOrWhiteSpace(userId))
      {
        MessageBox.Show("사번을 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      using (var client = new HttpClient())
      {
        try
        {
          var values = new Dictionary<string, string>
            {
                { "id", userId },
                { "company", company }
            };
          var content = new FormUrlEncodedContent(values);
          HttpResponseMessage response = await client.PostAsync("http://10.14.42.145:9091/member/loginAPI", content);

          // 응을 문자열로 받음
          var responseText = await response.Content.ReadAsStringAsync();

          // 보기 좋게 출력 (디버깅용)
          var parsedJson = JsonDocument.Parse(responseText).RootElement;
          string formatted = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions
          {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
          });
          // → 이 부분은 디버깅 끝났으면 제거 가능!
          // MessageBox.Show(formatted, "서버 응답", MessageBoxButtons.OK, MessageBoxIcon.Information);

          // 파싱하여 객체화
          using var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseText));
          var loginResult = await JsonSerializer.DeserializeAsync<MemberLoginResult>(responseStream);

          if (loginResult != null && loginResult.Success)
          {
            LogInStateText.Text = $"반갑습니다. {loginResult.UserDept} {loginResult.UserName}님";
            LogInStateText.ForeColor = Color.Blue;
            // 로그인 성공했으므로 버튼 조건 재확인
            SuccessButtonStates();

            string filePath = @"C:\temp\BeamStructureSolutionUser.txt";
            string userInfo = $"userID: {loginResult.UserID}\n" +
                              $"userName: {loginResult.UserName}\n" +
                              $"userDept: {loginResult.UserDept}\n" +
                              $"userPos: {loginResult.UserPos}\n" +
                              $"userCompany: {loginResult.UserCompany}";

            // C:\temp 디렉토리가 없으면 생성
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, userInfo);
          }
          else
          {
            if (loginResult?.Reason == "no_permission")
            {
              MessageBox.Show("HiTESS Cloud에 접속하여 프로그램 사 권한을 요청하세요\n '회원가입' 버튼 클릭하면 HiTESS Cloud로 이동합니다", "권한 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
              LogInStateText.Text = $"로그인 실패, 회원 가입 해주세요.";
              LogInStateText.ForeColor = Color.Red;
              FailButtonStates();
            }
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"로그인 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }

    private async void joinButton_Click(object sender, EventArgs e)
    {
      string url = "http://10.14.42.145:9091/member/join";

      try
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = url,
          UseShellExecute = true // 👉 반드시 있어야 브라우저에서 열림
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show($"웹페이지 열기 실패: {ex.Message}");
      }
    }

    private void DrawShearForce(object sender, PaintEventArgs e)
    {
      Graphics g = e.Graphics;
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.Clear(Color.White);

      if (!hasShearResult || shearResult == null || shearResult.Count < 2)
      {
        string msg = "해석 결과 확인 버튼 클릭 시, 확인 가능";
        using var mf = new Font("Segoe UI", 12, FontStyle.Italic);
        SizeF sz = g.MeasureString(msg, mf);
        g.DrawString(msg, mf, Brushes.Gray,
                     (shearForceBox.Width - sz.Width) / 2,
                     (shearForceBox.Height - sz.Height) / 2);
        return;
      }

      int width = shearForceBox.Width;
      int height = shearForceBox.Height;
      int leftMargin = 70;
      int rightMargin = 20;            // ← 추가된 오른쪽 여유
      int bottomMargin = 50;
      int graphTop = 10;
      int graphHeight = height - bottomMargin - graphTop;
      int graphBottom = graphTop + graphHeight;
      int graphCenterY = graphTop + graphHeight / 2;
      int halfGraphH = graphHeight / 2;
      int graphWidth = width - leftMargin - rightMargin;  // ← 전체 너비에서 좌우 마진 제외

      var font = new Font("Segoe UI", 10);
      var axisPen = new Pen(Color.Gray, 1);
      var gridPen = Pens.LightGray;
      var linePen = new Pen(Color.MediumBlue, 3);

      // Y범위 계산
      double maxShear = shearResult.Max(p => Math.Max(p.startShear, p.endShear));
      double minShear = shearResult.Min(p => Math.Min(p.startShear, p.endShear));
      double absMax = Math.Max(Math.Abs(maxShear), Math.Abs(minShear));
      int roundedMax = (int)Math.Ceiling(absMax / 10.0) * 10;
      double yRange = roundedMax * 1.5;

      double length = double.TryParse(lengthBox.Text, out double L) ? L : 1;

      // 1) 세로격자 & X레이블
      for (int i = 0; i <= 10; i++)
      {
        int x = leftMargin + i * graphWidth / 10;
        g.DrawLine(gridPen, x, graphTop, x, graphBottom);

        string lbl = (i * (length / 10)).ToString("F0");
        SizeF sz = g.MeasureString(lbl, font);
        g.DrawString(lbl, font, Brushes.Black,
                     x - sz.Width / 2,
                     graphBottom + 2);
      }

      // 2) 가로격자 & Y레이블
      for (int i = 0; i <= 10; i++)
      {
        int y = graphTop + i * graphHeight / 10;
        g.DrawLine(gridPen, leftMargin, y, leftMargin + graphWidth, y);

        double v = yRange - i * (2 * yRange / 10);
        string lbl = v.ToString("F0");
        SizeF sz = g.MeasureString(lbl, font);

        g.FillRectangle(Brushes.White,
            leftMargin - sz.Width - 5,
            y - sz.Height / 2,
            sz.Width,
            sz.Height);
        g.DrawString(lbl, font, Brushes.Black,
                     leftMargin - sz.Width - 5,
                     y - sz.Height / 2);
      }

      // 3) 0선
      g.DrawLine(axisPen,
          leftMargin, graphCenterY,
          leftMargin + graphWidth, graphCenterY);

      // 4) 계단형 그래프
      for (int i = 0; i < shearResult.Count - 1; i++)
      {
        var (p1, s1, e1) = shearResult[i];
        var (p2, s2, e2) = shearResult[i + 1];

        float px1 = leftMargin + (float)(p1 / length * graphWidth);
        float px2 = leftMargin + (float)(p2 / length * graphWidth);
        float pyS1 = graphCenterY - (float)(s1 / yRange * halfGraphH);
        float pyE1 = graphCenterY - (float)(e1 / yRange * halfGraphH);
        float pyS2 = graphCenterY - (float)(s2 / yRange * halfGraphH);
        float pyE2 = graphCenterY - (float)(e2 / yRange * halfGraphH);

        if (i == 0)
          g.DrawLine(linePen, px1, pyS1, px1, pyE1);

        g.DrawLine(linePen, px1, pyE1, px2, pyS2);
        g.DrawLine(linePen, px2, pyS2, px2, pyE2);
      }

      // 5) 제목
      using (var tf = new Font("Segoe UI", 11, FontStyle.Bold))
      {
        g.DrawString("Shear Force (N)", tf, Brushes.Black,
                     leftMargin + 5, graphTop);
      }
    }

    private void DrawBendingMoment(object sender, PaintEventArgs e)
    {
      // 0. 초기화
      Graphics g = e.Graphics;
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.Clear(Color.White);

      // 결과 유무 체크
      if (!hasMomentResult || momentResult == null || momentResult.Count < 2)
      {
        string msg = "해석 결과 확인 버튼 클릭 시, 확인 가능";
        using (var msgFont = new Font("Segoe UI", 12, FontStyle.Italic))
        {
          SizeF sz = g.MeasureString(msg, msgFont);
          g.DrawString(msg, msgFont, Brushes.Gray,
                       (bendingMomentBox.Width - sz.Width) / 2,
                       (bendingMomentBox.Height - sz.Height) / 2);
        }
        return;
      }

      // 1. 치수 및 마진 정의
      int width = bendingMomentBox.Width;
      int height = bendingMomentBox.Height;
      int leftMargin = 70;
      int rightMargin = 20;      // ← 오른쪽 여유
      int bottomMargin = 30;
      int graphTop = 10;
      int graphHeight = height - bottomMargin - graphTop;
      int graphBottom = graphTop + graphHeight;
      int graphCenterY = graphTop + graphHeight / 2;
      int halfGraphH = graphHeight / 2;
      int graphWidth = width - leftMargin - rightMargin;

      // 2. 펜·폰트 준비
      var font = new Font("Segoe UI", 10);
      var axisPen = new Pen(Color.Gray, 1);
      var gridPen = Pens.LightGray;
      var linePen = new Pen(Color.IndianRed, 3);

      // 3. Y 범위 계산 (절대 최대값 10단위 올림 + 1.2배 여유)
      double maxM = momentResult.Max(p => Math.Max(p.startMoment, p.endMoment));
      double absMax = Math.Max(Math.Abs(maxM), Math.Abs(momentResult.Min(p => Math.Min(p.startMoment, p.endMoment))));
      int roundedMax = (int)Math.Ceiling(absMax / 10.0) * 10;
      double yRange = roundedMax * 1.2;

      // 4. Beam 길이
      double length = double.TryParse(lengthBox.Text, out double L) ? L : 1;

      // ── 그리드 & 레이블 ──

      // 5. 세로 격자 + X축 레이블
      for (int i = 0; i <= 10; i++)
      {
        int x = leftMargin + i * graphWidth / 10;
        g.DrawLine(gridPen, x, graphTop, x, graphBottom);

        string lbl = (i * (length / 10)).ToString("F0");
        SizeF sz = g.MeasureString(lbl, font);
        g.DrawString(lbl, font, Brushes.Black,
                     x - sz.Width / 2,
                     graphBottom + 2);
      }

      // 6. 가로 격자 + Y축 레이블
      for (int i = 0; i <= 10; i++)
      {
        int y = graphTop + i * graphHeight / 10;
        g.DrawLine(gridPen, leftMargin, y, leftMargin + graphWidth, y);

        double v = yRange - i * (2 * yRange / 10);
        string lbl = v.ToString("F0");
        SizeF sz = g.MeasureString(lbl, font);

        // 백그라운드 지우기
        g.FillRectangle(Brushes.White,
            leftMargin - sz.Width - 5,
            y - sz.Height / 2,
            sz.Width, sz.Height);
        g.DrawString(lbl, font, Brushes.Black,
                     leftMargin - sz.Width - 5,
                     y - sz.Height / 2);
      }

      // 7. 0 기준선
      g.DrawLine(axisPen,
          leftMargin, graphCenterY,
          leftMargin + graphWidth, graphCenterY);

      // ── 빙딩 모멘트 계단형 그래프 ──

      for (int i = 0; i < momentResult.Count - 1; i++)
      {
        var (pos1, sM1, eM1) = momentResult[i];
        var (pos2, sM2, eM2) = momentResult[i + 1];

        float px1 = leftMargin + (float)(pos1 / length * graphWidth);
        float px2 = leftMargin + (float)(pos2 / length * graphWidth);

        float pyS1 = graphCenterY - (float)(sM1 / yRange * halfGraphH);
        float pyE1 = graphCenterY - (float)(eM1 / yRange * halfGraphH);
        float pyS2 = graphCenterY - (float)(sM2 / yRange * halfGraphH);
        float pyE2 = graphCenterY - (float)(eM2 / yRange * halfGraphH);

        // 첫 구간 수직선
        if (i == 0)
          g.DrawLine(linePen, px1, pyS1, px1, pyE1);

        // 계단 수평 + 다음 구간 수직
        g.DrawLine(linePen, px1, pyE1, px2, pyS2);
        g.DrawLine(linePen, px2, pyS2, px2, pyE2);
      }

      // 8. 제목
      using (var tf = new Font("Segoe UI", 11, FontStyle.Bold))
      {
        g.DrawString("Bending Moment (N·mm)", tf, Brushes.Black,
                     leftMargin + 5, graphTop);
      }
    }

    private void splitContainer1_Paint(object sender, PaintEventArgs e)
    {
      SplitContainer sc = sender as SplitContainer;

      // Splitter 사각형 정의
      Rectangle splitterRect;

      if (sc.Orientation == Orientation.Vertical)
      {
        splitterRect = new Rectangle(
            sc.SplitterDistance, 0,
            sc.SplitterWidth, sc.Height);
      }
      else
      {
        splitterRect = new Rectangle(
            0, sc.SplitterDistance,
            sc.Width, sc.SplitterWidth);
      }

      // 구분선 색상 설정 (DarkGray)
      e.Graphics.FillRectangle(Brushes.DarkGray, splitterRect);
    }

    private void SuccessButtonStates()
    {
      showChartButton.Enabled = true;
      runButton.Enabled = true;
    }

    private void FailButtonStates()
    {
      showChartButton.Enabled = false;
      runButton.Enabled = false;
    }

  
    private void BeamModelPanel_Paint(object sender, PaintEventArgs e)
    {
      Graphics g = e.Graphics;
      int width = beamModelPanel.Width;
      int height = beamModelPanel.Height;

      int marginLeft = 70;   // 왼쪽 여백
      int marginRight = 50;
      int marginTop = 20;
      int marginBottom = 20;

      int beamY = height / 2;
      beamY = Math.Max(beamY, marginTop);
      beamY = Math.Min(beamY, height - marginBottom);

      g.Clear(Color.White);

      //  Panel 테두리
      //g.DrawRectangle(Pens.Gray, marginLeft / 2, marginTop / 2, width - marginLeft - marginRight, height - marginTop - marginBottom);

      if (!isBeamModelVisible)
      {
        string message = "Beam Model 확인 버튼 클릭 시, 확인 가능";
        using var mf = new Font("Segoe UI", 12, FontStyle.Italic);
        SizeF textSize = g.MeasureString(message, mf);
        g.DrawString(message, mf, Brushes.Gray,
            (width - textSize.Width) / 2,
            (height - textSize.Height) / 2);
        return;
      }

      //  Beam 선
      g.DrawLine(new Pen(Color.Black, 4), marginLeft, beamY, width - marginRight, beamY);

      //  경계조건
      foreach (var bc in boundaryConditions)
      {
        int x = (int)(marginLeft + (bc.pos / length) * (width - marginLeft - marginRight));

        if (bc.constraint.Equals("Fix", StringComparison.OrdinalIgnoreCase))
        {
          Point[] triangle = {
            new Point(x, beamY),
            new Point(x - 10, beamY + 20),
            new Point(x + 10, beamY + 20)
        };
          g.FillPolygon(Brushes.Blue, triangle);
        }
        else if (bc.constraint.Equals("Hinge", StringComparison.OrdinalIgnoreCase))
        {
          int radius = 8;
          g.FillEllipse(Brushes.Blue, x - radius, beamY, radius * 2, radius * 2);
        }
      }


      //  하중
      using (var arrowPen = new Pen(Color.Red, 4))  //  화살표 선 두께: 2px
      {
        foreach (var ld in loads)
        {
          int x = (int)(marginLeft + (ld.pos / length) * (width - marginLeft - marginRight));
          int arrowLength = (int)(Math.Min(Math.Abs(ld.mag), 100) / 100 * (beamY - marginTop - 20));
          bool isDown = ld.mag > 0;
          string magnitudeLabel = $"{Math.Abs(ld.mag)} N";

          // 화살촉 꼭지점이 beam에 닿도록
          Point[] arrowHead = isDown
              ? new Point[]
              {
            new Point(x, beamY),
            new Point(x - 5, beamY + 10),
            new Point(x + 5, beamY + 10)
              }
              : new Point[]
              {
            new Point(x, beamY),
            new Point(x - 5, beamY - 10),
            new Point(x + 5, beamY - 10)
              };

          // 화살대 (굵기 적용)
          int lineStartY = isDown ? beamY : beamY - arrowLength;
          int lineEndY = isDown ? beamY + arrowLength : beamY;
          g.DrawLine(arrowPen, x, lineStartY, x, lineEndY);

          // 화살촉 그리기
          g.FillPolygon(Brushes.Red, arrowHead);

          // 텍스트 위치: 오른쪽 위 또는 아래
          // 텍스트 위치 계산
          SizeF textSize = g.MeasureString(magnitudeLabel, this.Font);
          float labelY = isDown
              ? beamY + arrowLength + 5
              : beamY - arrowLength - textSize.Height - 5;
          float labelX = x - textSize.Width - 5;  // 왼쪽으로 이동


          using var boldFont = new Font(this.Font.FontFamily, 12, FontStyle.Bold);
          g.DrawString(magnitudeLabel, boldFont, Brushes.Red, labelX, labelY);
        }

      }
    }

  
    private void Form1_Load(object? sender, EventArgs e)
    {
      UserIdBox.Focus();
      try
      {
        if (!Directory.Exists(selectedFolderPath))
        {
          Directory.CreateDirectory(selectedFolderPath);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"초기 폴더 생성 실패: {ex.Message}");
      }

    }
    private void Form1_Shown(object? sender, EventArgs e)
    {
      UserIdBox.Focus();
    }

  }
}

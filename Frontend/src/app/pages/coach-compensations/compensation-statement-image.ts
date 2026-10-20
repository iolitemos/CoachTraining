import { CoachCompensationStatement } from '../../models/coach-compensation.model';
import { formatCoachName } from '../../shared/coach-name/coach-name.pipe';

const THAI_MONTHS = [
  'มกราคม',
  'กุมภาพันธ์',
  'มีนาคม',
  'เมษายน',
  'พฤษภาคม',
  'มิถุนายน',
  'กรกฎาคม',
  'สิงหาคม',
  'กันยายน',
  'ตุลาคม',
  'พฤศจิกายน',
  'ธันวาคม',
];
const THAI_MONTHS_SHORT = [
  'ม.ค.',
  'ก.พ.',
  'มี.ค.',
  'เม.ย.',
  'พ.ค.',
  'มิ.ย.',
  'ก.ค.',
  'ส.ค.',
  'ก.ย.',
  'ต.ค.',
  'พ.ย.',
  'ธ.ค.',
];
const FONT = '"Noto Sans Thai", Prompt, sans-serif';
const WIDTH = 1600;

export function createCompensationStatementCanvas(
  statement: CoachCompensationStatement,
): HTMLCanvasElement {
  const detailHeight = Math.max(statement.items.length, 1) * 76;
  const adjustmentHeight = statement.adjustmentReason ? 112 : 0;
  const height = 840 + detailHeight + adjustmentHeight;
  const canvas = document.createElement('canvas');
  canvas.width = WIDTH;
  canvas.height = height;

  const context = canvas.getContext('2d');
  if (!context) throw new Error('Canvas is not supported');

  context.fillStyle = '#f1f5f9';
  context.fillRect(0, 0, WIDTH, height);
  drawHeader(context, statement);
  drawSummary(context, statement);
  const detailTop = drawAdjustment(context, statement);
  drawDetails(context, statement, detailTop);
  drawFooter(context, height);
  return canvas;
}

function drawHeader(
  context: CanvasRenderingContext2D,
  statement: CoachCompensationStatement,
): void {
  const gradient = context.createLinearGradient(0, 0, WIDTH, 360);
  gradient.addColorStop(0, '#172554');
  gradient.addColorStop(1, '#1d4ed8');
  context.fillStyle = gradient;
  context.fillRect(0, 0, WIDTH, 360);

  context.fillStyle = '#93c5fd';
  context.beginPath();
  context.arc(1450, 70, 180, 0, Math.PI * 2);
  context.fill();
  context.fillStyle = 'rgba(255,255,255,0.08)';
  context.beginPath();
  context.arc(1320, 310, 250, 0, Math.PI * 2);
  context.fill();

  context.fillStyle = '#bfdbfe';
  context.font = `500 27px ${FONT}`;
  context.fillText('COACH COMPENSATION STATEMENT', 84, 78);
  context.fillStyle = '#ffffff';
  context.font = `700 58px ${FONT}`;
  context.fillText('สรุปค่าตอบแทนโค้ช', 84, 150);

  const coachName = formatCoachName(statement.coachNickname?.trim() || statement.coachFullName);
  context.font = `600 38px ${FONT}`;
  context.fillText(coachName, 84, 228);
  context.fillStyle = '#dbeafe';
  context.font = `400 29px ${FONT}`;
  context.fillText(
    `${statement.coachCode}  •  ${periodLabel(statement.year, statement.month)}`,
    84,
    280,
  );

  const status = statusStyle(statement.status);
  roundedRect(context, 1240, 235, 250, 64, 32, status.background);
  context.fillStyle = status.text;
  context.font = `600 27px ${FONT}`;
  context.textAlign = 'center';
  context.fillText(status.label, 1365, 277);
  context.textAlign = 'left';
}

function drawSummary(
  context: CanvasRenderingContext2D,
  statement: CoachCompensationStatement,
): void {
  const top = 400;
  const gap = 24;
  const cardWidth = (WIDTH - 168 - gap * 3) / 4;
  const cards = [
    { label: 'เงินเดือนพื้นฐาน', value: money(statement.baseSalary), color: '#0f172a' },
    {
      label: 'ค่าสอน Private',
      value: money(statement.privateCompensation),
      detail: `${statement.privateSessionCount} Session`,
      color: '#1d4ed8',
    },
    {
      label: 'ปรับเพิ่ม / ลด',
      value: signedMoney(statement.adjustmentAmount),
      color: statement.adjustmentAmount < 0 ? '#dc2626' : '#0f172a',
    },
    { label: 'ยอดสุทธิ', value: money(statement.netAmount), color: '#ffffff', emphasis: true },
  ];

  cards.forEach((card, index) => {
    const x = 84 + index * (cardWidth + gap);
    roundedRect(context, x, top, cardWidth, 166, 24, card.emphasis ? '#1d4ed8' : '#ffffff');
    context.fillStyle = card.emphasis ? '#bfdbfe' : '#64748b';
    context.font = `500 25px ${FONT}`;
    context.fillText(card.label, x + 28, top + 45);
    context.fillStyle = card.color;
    context.font = `700 36px ${FONT}`;
    context.fillText(card.value, x + 28, top + 102);
    if (card.detail) {
      context.fillStyle = '#64748b';
      context.font = `400 22px ${FONT}`;
      context.fillText(card.detail, x + 28, top + 139);
    }
  });
}

function drawAdjustment(
  context: CanvasRenderingContext2D,
  statement: CoachCompensationStatement,
): number {
  if (!statement.adjustmentReason) return 620;

  roundedRect(context, 84, 598, WIDTH - 168, 88, 20, '#fffbeb');
  context.fillStyle = '#92400e';
  context.font = `600 24px ${FONT}`;
  context.fillText('หมายเหตุการปรับยอด', 112, 634);
  context.fillStyle = '#78350f';
  context.font = `400 23px ${FONT}`;
  context.fillText(truncate(context, statement.adjustmentReason, WIDTH - 480), 360, 634);
  return 724;
}

function drawDetails(
  context: CanvasRenderingContext2D,
  statement: CoachCompensationStatement,
  top: number,
): void {
  context.fillStyle = '#0f172a';
  context.font = `700 31px ${FONT}`;
  context.fillText('รายละเอียดค่าสอน Private', 84, top);
  context.fillStyle = '#64748b';
  context.font = `400 22px ${FONT}`;
  context.fillText('คำนวณจาก Session ที่ล็อกแล้วและให้เครดิตผู้สอนจริง', 84, top + 38);

  const tableTop = top + 70;
  const columns = [84, 290, 1260, 1516];
  roundedRect(context, 84, tableTop, WIDTH - 168, 58, 16, '#dbeafe');
  const headers = ['วันที่ / เวลา', 'ผู้เรียน', 'Rate', 'ยอด'];
  context.fillStyle = '#1e3a8a';
  context.font = `600 23px ${FONT}`;
  headers.forEach((header, index) => {
    context.textAlign = index >= 2 ? 'right' : 'left';
    context.fillText(header, columns[index], tableTop + 38);
  });

  context.textAlign = 'left';
  if (statement.items.length === 0) {
    context.fillStyle = '#64748b';
    context.font = `400 24px ${FONT}`;
    context.fillText('ไม่มี Private Session ที่นำมาคำนวณ', 112, tableTop + 108);
    return;
  }

  statement.items.forEach((item, index) => {
    const y = tableTop + 58 + index * 76;
    context.fillStyle = index % 2 === 0 ? '#ffffff' : '#f8fafc';
    context.fillRect(84, y, WIDTH - 168, 76);
    context.strokeStyle = '#e2e8f0';
    context.beginPath();
    context.moveTo(84, y + 76);
    context.lineTo(WIDTH - 84, y + 76);
    context.stroke();

    context.fillStyle = '#0f172a';
    context.font = `500 22px ${FONT}`;
    context.textAlign = 'left';
    context.fillText(displayDate(item.sessionDate), columns[0], y + 31);
    context.fillStyle = '#64748b';
    context.font = `400 19px ${FONT}`;
    context.fillText(
      `${time(item.scheduledStartDateTime)}–${time(item.scheduledEndDateTime)}`,
      columns[0],
      y + 58,
    );

    context.fillStyle = '#334155';
    context.font = `400 22px ${FONT}`;
    context.fillText(truncate(context, item.participantNames || '-', 680), columns[1], y + 45);
    context.textAlign = 'right';
    context.fillText(money(item.privateRate), columns[2], y + 45);
    context.fillStyle = '#0f172a';
    context.font = `600 22px ${FONT}`;
    context.fillText(money(item.amount), columns[3], y + 45);
  });
  context.textAlign = 'left';
}

function drawFooter(context: CanvasRenderingContext2D, height: number): void {
  context.fillStyle = '#64748b';
  context.font = `400 19px ${FONT}`;
  context.fillText('เอกสารสรุปจากระบบ Coach Training', 84, height - 38);
  context.textAlign = 'right';
  context.fillText(`สร้างเมื่อ ${generatedAt()}`, WIDTH - 84, height - 38);
  context.textAlign = 'left';
}

function roundedRect(
  context: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number,
  color: string,
): void {
  context.beginPath();
  context.roundRect(x, y, width, height, radius);
  context.fillStyle = color;
  context.fill();
}

function statusStyle(status: string): { label: string; background: string; text: string } {
  if (status === 'Paid') return { label: 'จ่ายแล้ว', background: '#dcfce7', text: '#166534' };
  if (status === 'Confirmed')
    return { label: 'ยืนยันแล้ว', background: '#dbeafe', text: '#1e40af' };
  return { label: 'รอตรวจสอบ', background: '#fef3c7', text: '#92400e' };
}

function periodLabel(year: number, month: number): string {
  return `${THAI_MONTHS[month - 1] ?? ''} ${year}`.trim();
}

function money(value: number): string {
  return `฿${new Intl.NumberFormat('th-TH', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)}`;
}

function signedMoney(value: number): string {
  if (value > 0) return `+${money(value)}`;
  return money(value);
}

function displayDate(value: string): string {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return `${day} ${THAI_MONTHS_SHORT[month - 1] ?? ''} ${year}`;
}

function time(value: string): string {
  return /T(\d{2}:\d{2})/.exec(value)?.[1] ?? '-';
}

function generatedAt(): string {
  const now = new Date();
  return `${displayDate(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`)} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')} น.`;
}

function truncate(context: CanvasRenderingContext2D, value: string, maxWidth: number): string {
  if (context.measureText(value).width <= maxWidth) return value;
  let result = value;
  while (result.length > 0 && context.measureText(`${result}…`).width > maxWidth) {
    result = result.slice(0, -1);
  }
  return `${result}…`;
}

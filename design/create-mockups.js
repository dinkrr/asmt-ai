const colors = {
  ink: '#454C60', muted: '#81899D', faint: '#A8AFC0', line: '#E3E5EC',
  paper: '#FFFFFF', canvas: '#F2F3F8', nav: '#30313F', navSoft: '#E1E3EB',
  green: '#009FC6', paleGreen: '#E2F5FA', amber: '#A86811', paleAmber: '#FFF2DF',
  blue: '#009FC6', paleBlue: '#E2F5FA', coral: '#E96C61', paleCoral: '#FCEFEA'
};

function paint(hex) {
  const value = hex.slice(1);
  return [{ type: 'SOLID', color: { r: parseInt(value.slice(0, 2), 16) / 255, g: parseInt(value.slice(2, 4), 16) / 255, b: parseInt(value.slice(4, 6), 16) / 255 } }];
}

function box(parent, name, x, y, width, height, fill, radius = 0, stroke = null) {
  const node = figma.createRectangle();
  node.name = name;
  parent.appendChild(node);
  node.x = x; node.y = y; node.resize(width, height);
  node.fills = paint(fill);
  node.cornerRadius = radius;
  if (stroke) { node.strokes = paint(stroke); node.strokeWeight = 1; }
  return node;
}

function text(parent, name, value, x, y, width, size = 14, color = colors.ink, weight = 'Regular', family = 'Segoe UI') {
  const node = figma.createText();
  parent.appendChild(node);
  node.name = name;
  node.fontName = { family, style: weight };
  node.fontSize = size;
  node.characters = value;
  node.fills = paint(color);
  node.textAutoResize = 'NONE';
  const estimatedLines = Math.ceil(value.length / Math.max(1, width / (size * 0.62)));
  node.resize(width, Math.max(32, estimatedLines * size * 1.5 + 6));
  node.x = x; node.y = y;
  return node;
}

function line(parent, x, y, width) { box(parent, 'Divider', x, y, width, 1, colors.line); }
function label(parent, value, x, y, width, bg, fg) {
  box(parent, value + ' badge', x, y, width, 25, bg, 4);
  text(parent, value, value, x + 9, y + 4, width - 18, 11, fg, 'Semibold');
}
function button(parent, value, x, y, width, primary = false) {
  box(parent, value + ' button', x, y, width, 38, primary ? '#197A4C' : colors.paper, 5, primary ? null : colors.line);
  text(parent, value, value, x + 14, y + 9, width - 25, 13, primary ? colors.paper : colors.ink, 'Semibold');
}
function panel(parent, name, x, y, width, height) { box(parent, name, x, y, width, height, colors.paper, 6, colors.line); }

function screen(name, index, active, status) {
  const frame = figma.createFrame();
  frame.name = name;
  frame.resize(1440, 1000);
  frame.x = index * 1510; frame.y = 0;
  frame.fills = paint(colors.canvas);
  frame.clipsContent = true;
  box(frame, 'Global navigation', 0, 0, 1440, 52, colors.nav);
  box(frame, 'Brand mark', 18, 12, 27, 27, colors.paper, 14);
  text(frame, 'Brand symbol', 'A', 25, 16, 15, 15, colors.nav, 'Bold');
  text(frame, 'Product', 'PRE-ASSESSMENT', 53, 10, 175, 16, colors.paper, 'Bold');
  text(frame, 'Product descriptor', 'Digital Platform', 54, 29, 130, 9, '#D2D5DF');
  text(frame, 'Requests navigation', 'Requests', 265, 17, 100, 13, colors.paper, 'Semibold');
  box(frame, 'Navigation indicator', 264, 49, 67, 3, colors.green);
  box(frame, 'Avatar', 1366, 11, 30, 30, '#D9DBE4', 15);
  text(frame, 'Avatar initials', 'CH', 1373, 18, 20, 10, colors.nav, 'Bold');
  box(frame, 'Requests sidebar', 0, 52, 216, 948, colors.paper);
  text(frame, 'Requests heading', 'Requests', 16, 70, 120, 14, colors.ink, 'Semibold');
  box(frame, 'All filter', 15, 104, 35, 24, colors.green, 12);
  text(frame, 'All filter label', 'All', 25, 109, 20, 11, colors.paper, 'Bold');
  box(frame, 'My filter', 55, 104, 39, 24, '#D5D8E2', 12);
  text(frame, 'My filter label', 'My', 66, 109, 25, 11, colors.ink, 'Semibold');
  box(frame, 'My Team filter', 99, 104, 78, 24, '#D5D8E2', 12);
  text(frame, 'My Team filter label', 'My Team', 109, 109, 65, 11, colors.ink, 'Semibold');
  box(frame, 'Request search', 15, 141, 185, 28, colors.paper, 3, colors.line);
  text(frame, 'Request search hint', 'Search requests', 25, 147, 165, 11, colors.faint);
  box(frame, 'Request list background', 0, 185, 216, 815, colors.canvas);
  const requests = [['Alex Morgan', 'Senior Software Engineer', 'Assigned'], ['Jordan Lee', 'Software Engineer', 'Assigned'], ['Sam Rivera', 'Lead Software Engineer', 'Rework']];
  requests.forEach((request, position) => {
    const top = 200 + position * 102;
    box(frame, 'Request ' + position, 12, top, 192, 88, colors.paper, 4, position === 0 ? colors.green : colors.line);
    box(frame, 'Candidate avatar ' + position, 23, top + 16, 29, 29, '#E2E5EC', 15);
    text(frame, 'Request name ' + position, request[0], 61, top + 12, 135, 12, colors.green, 'Semibold');
    text(frame, 'Request title ' + position, request[1], 61, top + 33, 135, 10, colors.muted);
    text(frame, 'Request status ' + position, request[2], 61, top + 57, 110, 10, colors.ink);
  });
  box(frame, 'Request action bar', 216, 52, 1224, 45, colors.paper);
  text(frame, 'Back to requests', '<  Get to the Requests table', 252, 67, 330, 12, colors.green);
  text(frame, 'Request status', status, 1220, 67, 170, 11, colors.muted, 'Semibold');
  text(frame, 'Candidate', 'Alex Morgan', 252, 107, 400, 20, colors.green, 'Semibold');
  text(frame, 'Candidate details', 'Software Engineer II, .NET    /    Next title: Senior Software Engineer    /    Assigned', 252, 135, 930, 12, colors.muted);
  const tabs = [['General', 252], ['Justification (2)', 325], ['Session Feedback', 467], ['Participants', 623], ['History', 731], ['AI Review', 814], ['Session Questions', 906]];
  const selectedTab = active === 'Review queue' ? 'AI Review' : active === 'Session prep' ? 'Session Questions' : 'Session Feedback';
  for (const [tab, position] of tabs) {
    text(frame, tab + ' request tab', tab, position, 164, 145, 11, tab === selectedTab ? colors.green : colors.ink, 'Semibold');
    if (tab === selectedTab) box(frame, 'Selected tab indicator', position, 184, tab.length * 7, 2, colors.green);
  }
  line(frame, 252, 186, 1138);
  return frame;
}

const review = screen('Presentation review', 0, 'Review queue', 'REVIEW NEEDED');
label(review, 'AI DRAFT', 252, 194, 75, colors.paleBlue, colors.blue);
text(review, 'Review intro', 'Self-presentation analysis', 252, 232, 700, 20, colors.ink, 'Semibold');
text(review, 'Review subtitle', '.NET  /  A3 target  /  Generated from the uploaded presentation', 252, 262, 700, 13, colors.muted);
button(review, 'Regenerate', 875, 235, 122);
panel(review, 'Category analysis', 252, 306, 745, 334);
text(review, 'Category title', 'Category assessment', 274, 326, 390, 15, colors.ink, 'Semibold');
text(review, 'Category hint', '4 areas reviewed', 832, 329, 140, 12, colors.muted);
line(review, 274, 359, 701);
const reviewRows = [
  ['Development experience', 'PASS', 'Project ownership and API delivery are described.', colors.paleGreen, colors.green],
  ['Architecture on practice', 'THIN', 'Trade-offs are mentioned but need deeper examples.', colors.paleAmber, colors.amber],
  ['Engineering excellence', 'PASS', 'Testing and delivery practices have clear examples.', colors.paleGreen, colors.green],
  ['Leadership', 'THIN', 'Mentoring is mentioned; scope and outcomes are unclear.', colors.paleAmber, colors.amber]
];
reviewRows.forEach((row, index) => {
  const top = 374 + index * 62;
  text(review, 'Category ' + index, row[0], 274, top, 210, 13, colors.ink, 'Semibold');
  label(review, row[1], 505, top - 4, 58, row[3], row[4]);
  text(review, 'Category note ' + index, row[2], 580, top - 1, 374, 12, colors.muted);
  if (index < 3) line(review, 274, top + 43, 701);
});
panel(review, 'Probe list', 252, 658, 745, 163);
text(review, 'Probe title', 'Gaps to probe in session', 274, 679, 430, 15, colors.ink, 'Semibold');
text(review, 'Probe 1', '01    API versioning and compatibility decisions', 274, 715, 640, 13, colors.ink);
text(review, 'Probe 2', '02    Mentoring impact and feedback examples', 274, 744, 640, 13, colors.ink);
text(review, 'Probe source', 'Based on A3 required skills not evidenced in the presentation.', 274, 781, 650, 12, colors.muted);
panel(review, 'Presentation source', 1018, 194, 372, 263);
text(review, 'Source title', 'Source material', 1040, 215, 310, 15, colors.ink, 'Semibold');
box(review, 'Document icon', 1040, 255, 40, 46, colors.paleBlue, 4);
text(review, 'Document symbol', 'P', 1054, 266, 20, 18, colors.blue, 'Bold');
text(review, 'Presentation filename', 'Self-presentation.pptx', 1094, 255, 260, 13, colors.ink, 'Semibold');
text(review, 'Presentation details', '12 slides  /  uploaded today', 1094, 278, 250, 12, colors.muted);
line(review, 1040, 321, 328);
text(review, 'Evidence title', 'EVIDENCE', 1040, 340, 250, 10, colors.muted, 'Bold');
text(review, 'Evidence extract', '"Led the migration of a customer API to a new service."', 1040, 363, 310, 13, colors.ink);
text(review, 'Evidence link', 'View slide 6  ->', 1040, 416, 220, 12, colors.green, 'Semibold');
panel(review, 'Review decision', 1018, 477, 372, 237);
text(review, 'Decision title', 'Committee decision', 1040, 499, 300, 15, colors.ink, 'Semibold');
text(review, 'Decision description', 'Review the evidence and choose how to proceed.', 1040, 530, 305, 13, colors.muted);
label(review, 'AI SUGGESTS: PROCEED', 1040, 575, 190, colors.paleGreen, colors.green);
button(review, 'Send back', 1040, 633, 134);
button(review, 'Approve review', 1185, 633, 180, true);
text(review, 'Review note', 'The committee owns the decision.', 1040, 683, 300, 11, colors.muted);

const questions = screen('Session preparation', 1, 'Session prep', 'READY TO PREPARE');
label(questions, 'AI DRAFT', 252, 194, 75, colors.paleBlue, colors.blue);
text(questions, 'Questions intro', 'Question set', 252, 232, 500, 20, colors.ink, 'Semibold');
text(questions, 'Questions subtitle', 'Tailored to the presentation, A3 rubric, and approved gaps.', 252, 262, 710, 13, colors.muted);
button(questions, 'Regenerate', 875, 235, 122);
box(questions, 'Question tabs', 252, 307, 745, 42, colors.paper, 5, colors.line);
text(questions, 'Active question tab', 'Development', 271, 319, 140, 13, colors.green, 'Semibold');
text(questions, 'Architecture tab', 'Architecture', 410, 319, 130, 13, colors.muted);
text(questions, 'Quality tab', 'Quality', 543, 319, 90, 13, colors.muted);
text(questions, 'Leadership tab', 'Leadership', 648, 319, 130, 13, colors.muted);
box(questions, 'Tab indicator', 270, 345, 90, 3, colors.green);
panel(questions, 'Question list', 252, 365, 745, 426);
text(questions, 'Question section', 'Development experience', 274, 385, 350, 16, colors.ink, 'Semibold');
text(questions, 'Question section meta', 'EXPERT 1   /   15-17 MIN', 702, 389, 270, 10, colors.muted, 'Bold');
line(questions, 274, 420, 701);
const questionRows = [
  ['01', '.NET API DESIGN', 'Walk us through an API you helped design. What trade-offs did you consider?', 'Presentation: slide 6'],
  ['02', 'VERSIONING  /  GAP', 'How would you introduce a breaking API change without disrupting clients?', 'Required for A3  /  not mentioned'],
  ['03', 'TROUBLESHOOTING', 'Describe a production issue you investigated and how you found the cause.', 'Presentation: slide 8']
];
questionRows.forEach((row, index) => {
  const top = 438 + index * 112;
  text(questions, 'Question number ' + index, row[0], 274, top, 35, 12, colors.faint, 'Bold');
  text(questions, 'Question skill ' + index, row[1], 320, top, 560, 10, colors.green, 'Bold');
  text(questions, 'Question ' + index, row[2], 320, top + 22, 590, 14, colors.ink, 'Semibold');
  text(questions, 'Question source ' + index, row[3], 320, top + 68, 580, 11, colors.muted);
  if (index < 2) line(questions, 320, top + 96, 650);
});
button(questions, 'Edit questions', 252, 815, 150);
button(questions, 'Approve question set', 409, 815, 190, true);
panel(questions, 'Session overview', 1018, 194, 372, 276);
text(questions, 'Session title', 'Session plan', 1040, 215, 310, 15, colors.ink, 'Semibold');
text(questions, 'Session duration', 'Approx. 75 min', 1040, 247, 310, 13, colors.muted);
line(questions, 1040, 281, 328);
text(questions, 'Expert assignments title', 'EXPERT ASSIGNMENTS', 1040, 301, 300, 10, colors.muted, 'Bold');
text(questions, 'Expert assignment one', 'Expert 1', 1040, 330, 110, 13, colors.ink, 'Semibold');
text(questions, 'Expert assignment one detail', 'Development, architecture', 1150, 330, 200, 12, colors.muted);
text(questions, 'Expert assignment two', 'Expert 2', 1040, 365, 110, 13, colors.ink, 'Semibold');
text(questions, 'Expert assignment two detail', 'Quality, processes', 1150, 365, 200, 12, colors.muted);
text(questions, 'Expert assignment three', 'Committee head', 1040, 400, 110, 13, colors.ink, 'Semibold');
text(questions, 'Expert assignment three detail', 'Warm-up, leadership', 1150, 400, 200, 12, colors.muted);
panel(questions, 'Coverage', 1018, 490, 372, 205);
text(questions, 'Coverage title', 'Coverage to review', 1040, 511, 300, 15, colors.ink, 'Semibold');
label(questions, '2 GAPS INCLUDED', 1040, 549, 145, colors.paleAmber, colors.amber);
text(questions, 'Coverage description', 'Prioritize the questions that fit the session. Remaining probes can be kept as backups.', 1040, 590, 310, 13, colors.muted);
text(questions, 'Coverage link', 'View approved gaps  ->', 1040, 657, 260, 12, colors.green, 'Semibold');

const feedback = screen('Session feedback', 2, 'Feedback', 'DRAFT IN REVIEW');
label(feedback, 'AI DRAFT', 252, 194, 75, colors.paleBlue, colors.blue);
text(feedback, 'Feedback intro', 'Assessment feedback', 252, 232, 640, 20, colors.ink, 'Semibold');
text(feedback, 'Feedback subtitle', 'Drafted from the session transcript and A3 skill matrix.', 252, 262, 710, 13, colors.muted);
button(feedback, 'Regenerate', 875, 235, 122);
panel(feedback, 'Feedback draft', 252, 306, 745, 495);
text(feedback, 'Draft heading', 'Candidate feedback', 274, 326, 390, 15, colors.ink, 'Semibold');
button(feedback, 'Edit draft', 842, 318, 130);
line(feedback, 274, 370, 701);
text(feedback, 'Strengths heading', 'What went well', 274, 391, 390, 15, colors.ink, 'Semibold');
text(feedback, 'Strength one', 'API design  /  Explained compatibility choices using a project example.', 274, 426, 675, 13, colors.ink);
text(feedback, 'Strength two', 'Testing  /  Described a practical integration testing approach.', 274, 456, 675, 13, colors.ink);
line(feedback, 274, 492, 701);
text(feedback, 'Improvements heading', 'What to develop', 274, 513, 400, 15, colors.ink, 'Semibold');
text(feedback, 'Improvement one', 'Architecture  /  Expand on how alternatives are evaluated against constraints.', 274, 548, 675, 13, colors.ink);
text(feedback, 'Improvement two', 'Leadership  /  Provide clearer examples of mentoring outcomes.', 274, 578, 675, 13, colors.ink);
line(feedback, 274, 614, 701);
text(feedback, 'Plan heading', 'Development plan', 274, 636, 400, 15, colors.ink, 'Semibold');
text(feedback, 'Plan content', 'Practice documenting design trade-offs and mentoring goals with measurable outcomes.', 274, 670, 675, 13, colors.ink);
text(feedback, 'Development resource', 'Design trade-offs guide  ->', 274, 705, 280, 12, colors.green, 'Semibold');
text(feedback, 'Evidence limit note', 'Only topics covered in the transcript are included.', 274, 752, 650, 12, colors.muted);
button(feedback, 'Save draft', 252, 825, 130);
button(feedback, 'Review and finalize', 392, 825, 186, true);
panel(feedback, 'Feedback evidence', 1018, 194, 372, 337);
text(feedback, 'Evidence panel title', 'Evidence and confidence', 1040, 215, 310, 15, colors.ink, 'Semibold');
text(feedback, 'Transcript label', 'SESSION TRANSCRIPT', 1040, 253, 300, 10, colors.muted, 'Bold');
text(feedback, 'Transcript name', 'Session transcript.txt', 1040, 275, 300, 13, colors.ink, 'Semibold');
text(feedback, 'Transcript metadata', 'Uploaded today  /  48 min', 1040, 298, 300, 12, colors.muted);
line(feedback, 1040, 330, 328);
text(feedback, 'Feedback quote', '"We kept the old endpoint available while clients migrated."', 1040, 352, 310, 13, colors.ink);
text(feedback, 'Feedback quote source', 'API design  /  18:42  ->', 1040, 408, 300, 12, colors.green, 'Semibold');
label(feedback, 'NOT OBSERVED', 1040, 452, 130, colors.paleAmber, colors.amber);
text(feedback, 'Not observed explanation', 'Cloud operations were not discussed.', 1040, 487, 300, 12, colors.muted);
panel(feedback, 'Human recommendation', 1018, 551, 372, 211);
text(feedback, 'Recommendation heading', 'Committee recommendation', 1040, 572, 320, 15, colors.ink, 'Semibold');
text(feedback, 'Recommendation note', 'Select a recommendation after reviewing the evidence.', 1040, 604, 310, 13, colors.muted);
box(feedback, 'Recommendation select', 1040, 658, 326, 38, colors.paper, 4, colors.line);
text(feedback, 'Recommendation placeholder', 'Select recommendation     v', 1053, 668, 295, 13, colors.muted);
text(feedback, 'Recommendation footer', 'No decision has been made.', 1040, 714, 300, 12, colors.muted);

return figma.currentPage.children.map(node => ({ id: node.id, name: node.name }));
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


TOOL_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOL_DIR))

from rectify_paper import detect_paper_corners, order_corners, rectify


class RectifyPaperTests(unittest.TestCase):
    def test_order_corners_returns_clockwise_from_top_left(self):
        corners = np.float32([[380, 270], [40, 30], [20, 250], [360, 50]])

        ordered = order_corners(corners)

        np.testing.assert_allclose(
            ordered,
            np.float32([[40, 30], [360, 50], [380, 270], [20, 250]]),
        )

    def test_rectify_maps_detected_paper_to_landscape_output(self):
        source = np.zeros((300, 420, 3), dtype=np.uint8)
        expected_corners = np.int32([[40, 30], [360, 50], [380, 270], [20, 250]])
        cv2.fillConvexPoly(source, expected_corners, (255, 255, 255))
        cv2.line(source, (90, 180), (320, 195), (0, 0, 0), 3)

        result, corners = rectify(source)

        self.assertGreater(result.shape[1], result.shape[0])
        self.assertGreater(result[5, 5].mean(), 240)
        np.testing.assert_allclose(corners, expected_corners, atol=8)

    def test_detect_paper_corners_returns_none_without_quadrilateral(self):
        source = np.zeros((200, 300, 3), dtype=np.uint8)

        corners = detect_paper_corners(source)

        self.assertIsNone(corners)


if __name__ == "__main__":
    unittest.main()
